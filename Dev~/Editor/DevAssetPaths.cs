using Gley.NavigationSystem.Editor;
using UnityEditor.Compilation;

namespace Gley.NavigationSystem.Dev
{
    public class DevAssetPaths
    {
        private const string AssemblyName = "Gley.NavigationSystem.Editor.Dev";

        public string DevRoot { get; }
        public string ScenesFolder { get; }
        public string MaterialsFolder { get; }
        public string NavigationDataFolder { get; }
        public string DevTestMapFolder { get; }
        public string Dev5kFolder { get; }
        public string GraphicsFolder { get; }
        public string PrefabFolder { get; }
        public string TextureFolder { get; }
        public string PresetFolder { get; }
        public string RouteLineShaderPath { get; }

        public DevAssetPaths()
        {
            DevRoot = ResolveDevRoot();
            ScenesFolder = DevRoot + "/Scenes";
            MaterialsFolder = ScenesFolder + "/Materials";
            NavigationDataFolder = ScenesFolder + "/NavigationData";
            DevTestMapFolder = NavigationDataFolder + "/DevTestMap";
            Dev5kFolder = NavigationDataFolder + "/Dev5k";

            NavigationPackageLocator packageLocator = new NavigationPackageLocator();
            GraphicsFolder = packageLocator.AssetPath("Graphics");
            PrefabFolder = GraphicsFolder + "/Prefabs";
            TextureFolder = GraphicsFolder + "/Textures";
            PresetFolder = GraphicsFolder + "/Presets";
            RouteLineShaderPath = packageLocator.AssetPath("Runtime/Shaders/RouteLine.shader");
        }

        private string ResolveDevRoot()
        {
            string asmdefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(AssemblyName);
            if (string.IsNullOrEmpty(asmdefPath))
            {
                return string.Empty;
            }

            string editorFolder = GetFolder(asmdefPath.Replace('\\', '/'));
            return GetFolder(editorFolder);
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
