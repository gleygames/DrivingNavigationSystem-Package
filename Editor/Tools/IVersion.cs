namespace Gley.NavigationSystem.Editor
{
    public interface IVersion
    {
        string FolderName { get; }
        string LongVersion { get; }
        int ShortVersion { get; }
    }
}
