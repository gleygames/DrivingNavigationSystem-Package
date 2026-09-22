namespace Gley.NavigationSystem.Editor
{
    public readonly struct ImageGuidance
    {
        public ImageSizePlan Recommended2048 { get; }
        public ImageSizePlan Recommended4096 { get; }
        public string RatioText { get; }
        public float Ratio { get; }

        public ImageGuidance(ImageSizePlan recommended2048, ImageSizePlan recommended4096, string ratioText, float ratio)
        {
            Recommended2048 = recommended2048;
            Recommended4096 = recommended4096;
            RatioText = ratioText;
            Ratio = ratio;
        }
    }
}
