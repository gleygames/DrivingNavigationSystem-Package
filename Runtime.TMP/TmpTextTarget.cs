using System.Text;
using Gley.NavigationSystem;
using TMPro;
using UnityEngine;

namespace Gley.NavigationSystem.TMP
{
    public class TmpTextTarget : NavigationTextTarget
    {
        [SerializeField] private TMP_Text text;

        public override void SetText(StringBuilder value)
        {
            text.SetText(value);
        }
    }
}
