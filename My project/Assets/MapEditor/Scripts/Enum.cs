using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapEditor
{
    /// <summary>
    /// 地形マスの種類を定義する列挙型
    /// </summary>
    public enum FieldTileType
    {
        /// <summary>未指定</summary>
        NONE = 0,

        /// <summary>道</summary>
        ROAD = 1,

        /// <summary>水</summary>
        WATER = 2,

        /// <summary>段差</summary>
        STEP = 3
    }

    /// <summary>
    /// ギミックマスの種類を定義する列挙型
    /// </summary>
    public enum GimmickTileType
    {
        /// <summary>空きマス</summary>
        NONE = 0,
        /// <summary>障害物マス</summary>
        OBSTACLE = 2,
        /// <summary>開始マス</summary>
        START = 3,
        /// <summary>ゴールマス</summary>
        GOAL = 4,
        /// <summary>アイテムマス</summary>
        ITEM = 5,
        /// <summary>キャラクターマス</summary>
        CHARACTER = 6,
        /// <summary>謎解きマス</summary>
        RIDDLE = 7,
        /// <summary>謎解き（回答なし）</summary>
        HINT = 8,
    }

    public static class EnumExtensions
    {
        static Dictionary<string, FieldTileType> FieldTileTypeMap = new()
        {
            { "none", FieldTileType.NONE },
            { "road", FieldTileType.ROAD },
            { "water", FieldTileType.WATER },
            { "step", FieldTileType.STEP }
        };

        static Dictionary<string, GimmickTileType> GimmickTileTypeMap = new()
        {
            { "none", GimmickTileType.NONE },
            { "obstacle", GimmickTileType.OBSTACLE },
            { "start", GimmickTileType.START },
            { "goal", GimmickTileType.GOAL },
            { "item", GimmickTileType.ITEM },
            { "character", GimmickTileType.CHARACTER },
            { "riddle", GimmickTileType.RIDDLE },
            { "hint", GimmickTileType.HINT }
        };

        public static FieldTileType GetFieldTileType(this Tilemap value, Vector3Int pos)
        {
            if (value == null || !value.HasTile(pos))
            {
                return FieldTileType.NONE;
            }

            var tile = value.GetTile<Tile>(pos);
            if (tile == null || tile.sprite == null || tile.sprite.texture == null)
                return FieldTileType.NONE;

            var texName = tile.sprite.texture.name.ToLower();
            return FieldTileTypeMap.GetValueOrDefault(texName, FieldTileType.NONE);
        }

        public static GimmickTileType GetGimmickTileType(this Tilemap value, Vector3Int pos)
        {
            if (value == null || !value.HasTile(pos))
            {
                return GimmickTileType.NONE;
            }

            var tile = value.GetTile<Tile>(pos);
            if (tile == null || tile.sprite == null || tile.sprite.texture == null)
                return GimmickTileType.NONE;

            var texName = tile.sprite.texture.name.ToLower();
            return GimmickTileTypeMap.GetValueOrDefault(texName, GimmickTileType.NONE);
        }

        public static bool GetDefaultIsMovable(this FieldTileType value)
        {
            return value switch
            {
                FieldTileType.NONE => true,
                FieldTileType.ROAD => true,
                FieldTileType.WATER => false,
                FieldTileType.STEP => false,
                _ => true
            };
        }
    }
}