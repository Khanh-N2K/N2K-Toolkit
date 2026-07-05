#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

namespace N2K
{
    public sealed class SubclassSelectorAutoFixMissingAttributesInCode : AssetPostprocessor
    {
        private static readonly Regex FieldRegex = new Regex(
            @"(?<attributes>(?:\s*\[[^\]]*\]\s*)+)(?<field>(?:(?:public|private|protected|internal|static|readonly|new)\s+)*[\w<>\.\?,\s\[\]]+\s+\w+\s*(?:=\s*[^;]+)?;)",
            RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly Regex SubclassSelectorAttributeRegex = new Regex(
            @"\[(?<content>[^\]]*\bSubclassSelector\b[^\]]*)\]",
            RegexOptions.Compiled);

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            bool changedAnyFile = false;

            foreach (string assetPath in importedAssets)
            {
                if (!assetPath.EndsWith(".cs"))
                    continue;

                string fullPath = Path.GetFullPath(assetPath);

                if (!File.Exists(fullPath))
                    continue;

                string original = File.ReadAllText(fullPath);
                string patched = PatchSource(original);

                if (patched == original)
                    continue;

                File.WriteAllText(fullPath, patched);
                changedAnyFile = true;
            }

            if (changedAnyFile)
            {
                EditorApplication.delayCall += AssetDatabase.Refresh;
            }
        }

        private static string PatchSource(string source)
        {
            return FieldRegex.Replace(source, match =>
            {
                string attributes = match.Groups["attributes"].Value;
                string field = match.Groups["field"].Value;

                if (!attributes.Contains("SubclassSelector"))
                    return match.Value;

                if (attributes.Contains("SerializeReference"))
                    return match.Value;

                string patchedAttributes = SubclassSelectorAttributeRegex.Replace(
                    attributes,
                    attrMatch =>
                    {
                        string content = attrMatch.Groups["content"].Value.Trim();

                        return $"[SerializeReference, {content}]";
                    },
                    1);

                return patchedAttributes + field;
            });
        }
    }
}
#endif