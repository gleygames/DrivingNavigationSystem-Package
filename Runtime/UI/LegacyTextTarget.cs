using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem
{
    public class LegacyTextTarget : NavigationTextTarget
    {
        [SerializeField] private Text text;

        public override void SetText(StringBuilder value)
        {
            text.text = value.ToString();
        }
    }
}
