namespace Gley.NavigationSystem.Editor
{
    public interface IRoadImporter
    {
        string SourceTag { get; }
        string DisplayName { get; }

        bool IsAvailable();
        void Import(RoadImportContext context);
    }
}
