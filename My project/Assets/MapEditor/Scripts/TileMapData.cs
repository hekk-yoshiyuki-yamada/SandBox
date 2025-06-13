using System.Collections.Generic;
using UnityEngine;

namespace MapEditor
{
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