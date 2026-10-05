using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem
{
    internal class NavigationTextOutput
    {
        internal bool CanWrite(Component target, NavigationTextWriter writer)
        {
            if (target == null)
            {
                return false;
            }

            if (target is Text)
            {
                return true;
            }

            return writer != null && writer.CanWrite(target);
        }

        internal void Write(Component target, NavigationTextWriter writer, StringBuilder text)
        {
            if (target == null)
            {
                return;
            }

            Text legacy = target as Text;
            if (legacy != null)
            {
                legacy.text = text.ToString();
                return;
            }

            if (writer != null)
            {
                writer.Write(target, text);
            }
        }

        internal Component FindText(GameObject root, NavigationTextWriter writer)
        {
            Text legacy = root.GetComponentInChildren<Text>(true);
            if (legacy != null)
            {
                return legacy;
            }

            if (writer != null)
            {
                return writer.FindText(root);
            }

            return null;
        }
    }
}
