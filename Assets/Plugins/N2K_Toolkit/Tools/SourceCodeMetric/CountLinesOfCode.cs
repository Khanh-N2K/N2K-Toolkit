#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace N2K
{
    public static class CountLinesOfCode
    {
        public struct LineCountResult
        {
            public int CodeLines;
            public int CommentLines;
            public int BlankLines;
            public int TotalLines;
        }

        public static LineCountResult CountLines(List<string> foldersToIgnore = null)
        {
            string assetsPath = Application.dataPath.Replace("\\", "/");
            string[] scripts = Directory.GetFiles(assetsPath, "*.cs", SearchOption.AllDirectories);

            List<string> ignorePaths = new List<string>();
            if (foldersToIgnore != null)
            {
                foreach (var folder in foldersToIgnore)
                {
                    string path = Path.Combine(assetsPath, folder).Replace("\\", "/");
                    if (Directory.Exists(path))
                        ignorePaths.Add(path);
                }
            }

            int codeLines = 0;
            int commentLines = 0;
            int blankLines = 0;

            foreach (string script in scripts)
            {
                string normalizedPath = script.Replace("\\", "/");

                bool ignored = false;
                foreach (string ignore in ignorePaths)
                {
                    if (normalizedPath.StartsWith(ignore))
                    {
                        ignored = true;
                        break;
                    }
                }

                if (ignored) continue;

                bool inBlockComment = false;
                string[] lines = File.ReadAllLines(script);

                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim();

                    if (string.IsNullOrEmpty(line))
                    {
                        blankLines++;
                        continue;
                    }

                    if (inBlockComment)
                    {
                        commentLines++;
                        if (line.Contains("*/"))
                            inBlockComment = false;
                        continue;
                    }

                    if (line.StartsWith("//"))
                    {
                        commentLines++;
                    }
                    else if (line.StartsWith("/*"))
                    {
                        commentLines++;
                        if (!line.Contains("*/"))
                            inBlockComment = true;
                    }
                    else
                    {
                        codeLines++;
                    }
                }
            }

            return new LineCountResult
            {
                CodeLines = codeLines,
                CommentLines = commentLines,
                BlankLines = blankLines,
                TotalLines = codeLines + commentLines + blankLines
            };
        }
    }
}
#endif