using UnityEditor;
using UnityEditor.Build;

namespace Gley.NavigationSystem.Editor
{
    public class PreprocessorDirective
    {
        public void AddToCurrent(string directive, bool remove)
        {
            AddToPlatform(directive, remove, EditorUserBuildSettings.selectedBuildTargetGroup);
        }

        private void AddToPlatform(string directive, bool remove, BuildTargetGroup target)
        {
            NamedBuildTarget buildTarget = NamedBuildTarget.FromBuildTargetGroup(target);
            string textToWrite = PlayerSettings.GetScriptingDefineSymbols(buildTarget);

            if (remove)
            {
                if (textToWrite.Contains(directive))
                {
                    textToWrite = textToWrite.Replace(directive, "");
                }
            }
            else
            {
                if (!textToWrite.Contains(directive))
                {
                    if (textToWrite == "")
                    {
                        textToWrite += directive;
                    }
                    else
                    {
                        textToWrite += "," + directive;
                    }
                }
            }

            PlayerSettings.SetScriptingDefineSymbols(buildTarget, textToWrite);
        }
    }
}
