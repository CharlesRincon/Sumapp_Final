namespace Networking.Services
{
    public enum SliceTileType
    {
        Hydric,
        Catastrophic
    }

    /// <summary>
    /// Minimal tile resolver for the one-round vertical slice.
    /// </summary>
    public class TileService
    {
        public SliceTileType GetTileType(int boardPosition)
        {
            // Simple deterministic board layout for testability.
            return boardPosition % 4 == 0 ? SliceTileType.Catastrophic : SliceTileType.Hydric;
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
