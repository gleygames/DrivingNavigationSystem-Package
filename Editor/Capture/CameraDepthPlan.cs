namespace Gley.NavigationSystem.Editor
{
    public readonly struct CameraDepthPlan
    {
        public float CameraY { get; }
        public float Near { get; }
        public float Far { get; }

        public CameraDepthPlan(float cameraY, float near, float far)
        {
            CameraY = cameraY;
            Near = near;
            Far = far;
        }
    }
}
