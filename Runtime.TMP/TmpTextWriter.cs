using System.Text;
using TMPro;
using UnityEngine;

namespace Gley.NavigationSystem.TMP
{
    [CreateAssetMenu(fileName = "TmpTextWriter", menuName = "Gley/Navigation System/TMP Text Writer")]
    public class TmpTextWriter : NavigationTextWriter
    {
        public override bool CanWrite(Component target)
        {
            return target is TMP_Text;
        }

        public override void Write(Component target, StringBuilder text)
        {
            TMP_Text tmp = target as TMP_Text;
            if (tmp != null)
            {
                tmp.SetText(text);
            }
        }

        public override Component FindText(GameObject root)
        {
            return root.GetComponentInChildren<TMP_Text>(true);
        }
    }
}
