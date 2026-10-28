using UnityEditor.Compilation;

namespace Gley.NavigationSystem.Editor
{
    internal class NavigationPackageLocator
    {
        private const string AssemblyName = "Gley.NavigationSystem.Editor";

        public string PackageRoot()
        {
            string asmdefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(AssemblyName);
            if (string.IsNullOrEmpty(asmdefPath))
            {
                return string.Empty;
            }

            string editorFolder = GetFolder(asmdefPath.Replace('\\', '/'));
            return GetFolder(editorFolder);
        }

        public string AssetPath(string relativeToRoot)
        {
            return PackageRoot() + "/" + relativeToRoot;
        }

        private string GetFolder(string assetPath)
        {
            int separatorIndex = assetPath.LastIndexOf('/');
            if (separatorIndex <= 0)
            {
                return string.Empty;
            }

            return assetPath.Substring(0, separatorIndex);
        }
    }
}
