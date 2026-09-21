namespace Gley.NavigationSystem.Editor
{
    public class NavigationMapAssets
    {
        public NavigationMapAssets(MapData mapAsset, RoadNetworkAuthoring authoringAsset, RoadNetworkData runtimeAsset)
        {
            MapAsset = mapAsset;
            AuthoringAsset = authoringAsset;
            RuntimeAsset = runtimeAsset;
        }

        public MapData MapAsset { get; }
        public RoadNetworkAuthoring AuthoringAsset { get; }
        public RoadNetworkData RuntimeAsset { get; }
    }
}
