using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gley.NavigationSystem.Editor
{
    public class UrpCaptureAdapter : ICapturePipelineAdapter
    {
        public bool SupportsExposure { get { return false; } }

        public bool IsActive()
        {
            return GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset;
        }

        public void Prepare(Camera camera, CaptureSettings settings)
        {
            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            CopyOwnCameraData(cameraData, settings);
            cameraData.renderPostProcessing = !settings.DisablePostEffects;
        }

        public void Restore()
        {
        }

        public bool TryGetDefaultExposure(out float ev)
        {
            ev = 0f;
            return false;
        }

        private void CopyOwnCameraData(UniversalAdditionalCameraData cameraData, CaptureSettings settings)
        {
            if (!settings.UseOwnCamera || settings.OwnCamera == null)
            {
                return;
            }

            UniversalAdditionalCameraData ownData = settings.OwnCamera.GetComponent<UniversalAdditionalCameraData>();
            if (ownData == null)
            {
                return;
            }

            cameraData.renderShadows = ownData.renderShadows;
            cameraData.volumeLayerMask = ownData.volumeLayerMask;
            cameraData.antialiasing = ownData.antialiasing;
        }
    }
}
