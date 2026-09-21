using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Gley.NavigationSystem.Editor
{
    public class HdrpCaptureAdapter : ICapturePipelineAdapter
    {
        private const string VolumeObjectName = "Navigation Capture Volume";
        private const float VolumePriority = 100000f;

        private GameObject volumeObject;
        private VolumeProfile profile;

        public bool SupportsExposure { get { return true; } }

        public bool IsActive()
        {
            return GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset;
        }

        public void Prepare(Camera camera, CaptureSettings settings)
        {
            Restore();

            volumeObject = new GameObject(VolumeObjectName);
            volumeObject.hideFlags = HideFlags.HideAndDontSave;

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.hideFlags = HideFlags.HideAndDontSave;

            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = VolumePriority;
            volume.sharedProfile = profile;

            Exposure exposure = profile.Add<Exposure>(true);
            exposure.mode.value = ExposureMode.Fixed;
            exposure.fixedExposure.value = settings.HdrpExposure;

            if (settings.DisablePostEffects)
            {
                AddDisabledPostEffects();
            }
        }

        public void Restore()
        {
            if (profile != null)
            {
                List<VolumeComponent> components = profile.components;
                for (int i = 0; i < components.Count; i++)
                {
                    if (components[i] != null)
                    {
                        Object.DestroyImmediate(components[i]);
                    }
                }
                components.Clear();
                Object.DestroyImmediate(profile);
                profile = null;
            }

            if (volumeObject != null)
            {
                Object.DestroyImmediate(volumeObject);
                volumeObject = null;
            }
        }

        public bool TryGetDefaultExposure(out float ev)
        {
            ev = 0f;
            bool found = false;
            float bestPriority = float.MinValue;

            Volume[] volumes = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);
            for (int i = 0; i < volumes.Length; i++)
            {
                Volume volume = volumes[i];
                if (!volume.isActiveAndEnabled || !volume.isGlobal || volume.sharedProfile == null)
                {
                    continue;
                }
                if (found && volume.priority < bestPriority)
                {
                    continue;
                }

                Exposure exposure;
                if (!volume.sharedProfile.TryGet(out exposure))
                {
                    continue;
                }
                if (!exposure.active || !exposure.mode.overrideState || exposure.mode.value != ExposureMode.Fixed)
                {
                    continue;
                }

                ev = exposure.fixedExposure.value;
                bestPriority = volume.priority;
                found = true;
            }

            return found;
        }

        private void AddDisabledPostEffects()
        {
            Bloom bloom = profile.Add<Bloom>(true);
            bloom.intensity.value = 0f;

            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.value = 0f;

            MotionBlur motionBlur = profile.Add<MotionBlur>(true);
            motionBlur.intensity.value = 0f;

            DepthOfField depthOfField = profile.Add<DepthOfField>(true);
            depthOfField.focusMode.value = DepthOfFieldMode.Off;

            ChromaticAberration chromaticAberration = profile.Add<ChromaticAberration>(true);
            chromaticAberration.intensity.value = 0f;

            FilmGrain filmGrain = profile.Add<FilmGrain>(true);
            filmGrain.intensity.value = 0f;

            LensDistortion lensDistortion = profile.Add<LensDistortion>(true);
            lensDistortion.intensity.value = 0f;
        }
    }
}
