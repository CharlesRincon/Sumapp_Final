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

        [Header("Tile Colors")]
        [SerializeField] private Color _startTileColor = Color.white;
        [SerializeField] private Color _hydricTileColor = new Color(0.27f, 0.72f, 0.95f, 1f);
        [SerializeField] private Color _catastrophicTileColor = new Color(0.95f, 0.33f, 0.27f, 1f);
        [SerializeField] private Color _projectTileColor = new Color(1f, 0.72f, 0.2f, 1f);
        [SerializeField] private Color _drawCardTileColor = new Color(0.65f, 0.45f, 0.95f, 1f);
        [SerializeField] private Color _triviaTileColor = new Color(0.35f, 0.84f, 0.51f, 1f);

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

        public Color GetTileColor(int boardPosition)
        {
            return GetTileColor(GetTileType(boardPosition));
        }

        public Color GetTileColor(Networking.Services.SliceTileType tileType)
        {
            switch (tileType)
            {
                case Networking.Services.SliceTileType.Start:
                    return _startTileColor;
                case Networking.Services.SliceTileType.Hydric:
                    return _hydricTileColor;
                case Networking.Services.SliceTileType.Catastrophic:
                    return _catastrophicTileColor;
                case Networking.Services.SliceTileType.Project:
                    return _projectTileColor;
                case Networking.Services.SliceTileType.DrawCard:
                    return _drawCardTileColor;
                case Networking.Services.SliceTileType.Trivia:
                    return _triviaTileColor;
                default:
                    return Color.white;
            }
        }
    }
}
