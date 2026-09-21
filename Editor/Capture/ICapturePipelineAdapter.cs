using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public interface ICapturePipelineAdapter
    {
        bool SupportsExposure { get; }

        bool IsActive();

        void Prepare(Camera camera, CaptureSettings settings);

        void Restore();

        bool TryGetDefaultExposure(out float ev);
    }
}
