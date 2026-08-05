using UnityEngine;

namespace N2K
{
    public class InfoBoxAttribute : PropertyAttribute
    {
        public readonly string message;
        public readonly InforBoxMessageType type;

        public InfoBoxAttribute(string message, InforBoxMessageType type = InforBoxMessageType.Warning)
        {
            this.message = message;
            this.type = type;
        }
    }

    public enum InforBoxMessageType
    {
        None,
        Info,
        Warning,
        Error
    }
}