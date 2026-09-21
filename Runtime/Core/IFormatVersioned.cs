namespace Gley.NavigationSystem
{
    public interface IFormatVersioned
    {
        int FormatVersion { get; }
        int CurrentFormatVersion { get; }
    }
}
