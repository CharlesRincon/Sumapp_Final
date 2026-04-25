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

        [SerializeField] private ColombiaZone[] _zones = new ColombiaZone[24]
        {
            ColombiaZone.Andean, ColombiaZone.Caribbean, ColombiaZone.Pacific, ColombiaZone.Orinoquia,
            ColombiaZone.Amazon, ColombiaZone.Insular, ColombiaZone.Andean, ColombiaZone.Caribbean,
            ColombiaZone.Pacific, ColombiaZone.Orinoquia, ColombiaZone.Amazon, ColombiaZone.Insular,
            ColombiaZone.Andean, ColombiaZone.Caribbean, ColombiaZone.Pacific, ColombiaZone.Orinoquia,
            ColombiaZone.Amazon, ColombiaZone.Insular, ColombiaZone.Andean, ColombiaZone.Caribbean,
            ColombiaZone.Pacific, ColombiaZone.Orinoquia, ColombiaZone.Amazon, ColombiaZone.Insular
        };

        public int TileCount => _tiles.Length;

        public Networking.Services.SliceTileType GetTileType(int boardPosition)
        {
            if (_tiles == null || _tiles.Length == 0)
                return Networking.Services.SliceTileType.Hydric;

            int index = ((boardPosition % _tiles.Length) + _tiles.Length) % _tiles.Length;
            return _tiles[index];
        }

        public ColombiaZone GetZone(int boardPosition)
        {
            if (_zones == null || _zones.Length == 0)
                return ColombiaZone.Andean;

            int index = ((boardPosition % _zones.Length) + _zones.Length) % _zones.Length;
            return _zones[index];
        }
    }
}
