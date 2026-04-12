using System;
using UnityEngine;

namespace Networking.Models
{
    [CreateAssetMenu(fileName = "BoardTileConfig", menuName = "Networking/Board Tile Config")]
    public class BoardTileConfig : ScriptableObject
    {
        [SerializeField] private Networking.Services.SliceTileType[] _tiles = new Networking.Services.SliceTileType[24]
        {
            Networking.Services.SliceTileType.Start,
            Networking.Services.SliceTileType.Hydric, Networking.Services.SliceTileType.Hydric, Networking.Services.SliceTileType.Hydric,
            Networking.Services.SliceTileType.Catastrophic, Networking.Services.SliceTileType.Hydric, Networking.Services.SliceTileType.Hydric,
            Networking.Services.SliceTileType.Hydric, Networking.Services.SliceTileType.Catastrophic, Networking.Services.SliceTileType.Hydric,
            Networking.Services.SliceTileType.Hydric, Networking.Services.SliceTileType.Hydric, Networking.Services.SliceTileType.Catastrophic,
            Networking.Services.SliceTileType.Hydric, Networking.Services.SliceTileType.Hydric, Networking.Services.SliceTileType.Hydric,
            Networking.Services.SliceTileType.Catastrophic, Networking.Services.SliceTileType.Hydric, Networking.Services.SliceTileType.Hydric,
            Networking.Services.SliceTileType.Hydric, Networking.Services.SliceTileType.Catastrophic, Networking.Services.SliceTileType.Hydric,
            Networking.Services.SliceTileType.Hydric, Networking.Services.SliceTileType.Hydric
        };

        public int TileCount => _tiles.Length;

        public Networking.Services.SliceTileType GetTileType(int boardPosition)
        {
            if (_tiles == null || _tiles.Length == 0)
                return Networking.Services.SliceTileType.Hydric;

            int index = ((boardPosition % _tiles.Length) + _tiles.Length) % _tiles.Length;
            return _tiles[index];
        }
    }
}
