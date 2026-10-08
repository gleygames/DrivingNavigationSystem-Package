namespace Gley.NavigationSystem
{
    public interface IMapMarkerVisual
    {
        void Bind(MapMarker marker, MapView view);
        void Unbind();
    }
}
