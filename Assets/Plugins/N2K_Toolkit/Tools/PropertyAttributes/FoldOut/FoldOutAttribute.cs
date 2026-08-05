using System;
using UnityEngine;

namespace N2K
{
    [AttributeUsage(AttributeTargets.Field)]
    public class FoldOutAttribute : PropertyAttribute
    {
        public string Name { get; private set; }
        public FoldOutAttribute(string name)
        {
            Name = name;
        }
    }
}
