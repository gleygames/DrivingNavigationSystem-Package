using System.Collections.Generic;
using Gley.Common;
using UnityEditor;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class MapImageImportSettings
    {
        public const int MobileMaxTextureSize = 4096;
        private const int MinTextureSize = 32;
        private const int MaxTextureSize = 16384;

        private readonly string[] mobilePlatforms;

        public MapImageImportSettings()
        {
            mobilePlatforms = new string[] { "Android", "iPhone" };
        }

        public void ApplyIfNew(string assetPath, bool isNewFile, int longerSidePixels)
        {
            if (!isNewFile)
            {
                return;
            }

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                CustomLogger.LogWarning("MapImageImportSettings: no texture importer found at " + assetPath + ".");
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.sRGBTexture = true;
            importer.isReadable = false;
            importer.maxTextureSize = GetMaxTextureSize(longerSidePixels);
            importer.textureCompression = TextureImporterCompression.Compressed;

            for (int i = 0; i < mobilePlatforms.Length; i++)
            {
                TextureImporterPlatformSettings platformSettings = importer.GetPlatformTextureSettings(mobilePlatforms[i]);
                platformSettings.overridden = true;
                platformSettings.maxTextureSize = MobileMaxTextureSize;
                importer.SetPlatformTextureSettings(platformSettings);
            }

            importer.SaveAndReimport();
        }

        public void GetWarnings(string assetPath, List<string> output)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            int width;
            int height;
            importer.GetSourceTextureWidthAndHeight(out width, out height);
            if (width % 4 != 0 || height % 4 != 0)
            {
                output.Add("Image size " + width + " x " + height + " is not a multiple of 4. Compression may add borders or blur.");
            }

            if (importer.npotScale != TextureImporterNPOTScale.None)
            {
                output.Add("Non Power of 2 scaling is on. Unity will rescale the image and it may no longer line up with the roads. Set it to None.");
            }

            if (!importer.mipmapEnabled)
            {
                output.Add("Mipmaps are off. The map will shimmer when zoomed out. Turn Generate Mip Maps on.");
            }

            if (HasMobileSizeAbove4096(importer))
            {
                output.Add("Mobile max size is above " + MobileMaxTextureSize + ". Some phones can't load it. Set the Android and iOS max size to " + MobileMaxTextureSize + ".");
            }
        }

        private int GetMaxTextureSize(int longerSidePixels)
        {
            int size = MinTextureSize;
            while (size < longerSidePixels && size < MaxTextureSize)
            {
                size *= 2;
            }
            return size;
        }

        private bool HasMobileSizeAbove4096(TextureImporter importer)
        {
            for (int i = 0; i < mobilePlatforms.Length; i++)
            {
                TextureImporterPlatformSettings platformSettings = importer.GetPlatformTextureSettings(mobilePlatforms[i]);
                int maxSize;
                if (platformSettings.overridden)
                {
                    maxSize = platformSettings.maxTextureSize;
                }
                else
                {
                    maxSize = importer.maxTextureSize;
                }

                if (maxSize > MobileMaxTextureSize)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
