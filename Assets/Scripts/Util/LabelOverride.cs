//https://discussions.unity.com/t/can-i-change-variable-name-on-inspector/143568/3

using UnityEngine;

namespace Zodiac.Util
{
    public class LabelOverride : PropertyAttribute
    {
        public string label;

        public LabelOverride(string label) => this.label = label;
    }
}