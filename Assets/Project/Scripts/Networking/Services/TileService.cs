namespace Networking.Services
{
    public enum SliceTileType
    {
        Start,
        Hydric,
        Catastrophic
    }

    /// <summary>
    /// Tile resolver that reads from a BoardTileConfig asset.
    /// Falls back to Hydric when no config is assigned.
    /// </summary>
    public class TileService
    {
        private readonly Networking.Models.BoardTileConfig _config;

        public TileService(Networking.Models.BoardTileConfig config)
        {
            _config = config;
        }

        public SliceTileType GetTileType(int boardPosition)
        {
            if (_config == null)
                return SliceTileType.Hydric;

            return _config.GetTileType(boardPosition);
        }

        public int ResolveHydricWaterDelta(int hydricGain)
        {
            return hydricGain;
        }

        public int ResolveCatastrophicWaterDelta(int catastrophicPenalty)
        {
            return -catastrophicPenalty;
        }

        public int ResolveHydricBasinDelta(int hydricBasinBonus)
        {
            return hydricBasinBonus;
        }

        public int ResolveCatastrophicBasinDelta(int catastrophicBasinPenalty)
        {
            return -catastrophicBasinPenalty;
        }
    }
}
