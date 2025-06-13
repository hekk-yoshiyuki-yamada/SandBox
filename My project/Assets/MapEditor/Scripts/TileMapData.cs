using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MapEditor
{
    /// <summary>
    /// ギミックマスの種類を定義する列挙型
    /// </summary>
    public enum GimmickTileType
    {
        /// <summary>空きマス</summary>
        NONE = 0,

        /// <summary>イベントマス</summary>
        EVENT = 1,

        /// <summary>障害物マス</summary>
        OBSTACLE = 2,

        /// <summary>開始マス</summary>
        START = 3,

        /// <summary>ゴールマス</summary>
        GOAL = 4,

        /// <summary>マップ移動マス</summary>
        TELEPORT = 5
    }

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
    /// タイルデータの定義(summer2025_hunt_tile)
    /// </summary>
    [System.Serializable]
    public class TileData
    {
        public int id;
        public int groupId;
        public FieldTileType fieldTileType;
        public GimmickTileType gimmickTileType;
        public int gimmickTileTypeValue = 0;
        public bool isMovable = true;
        public Vector3Int position;
    }

    [CreateAssetMenu(fileName = "TileMapData", menuName = "TileMap/TileMapData", order = 1)]
    public class TileMapData : ScriptableObject
    {
        public List<TileData> tilesData = new();
        public int groupId = 1;
    }
}