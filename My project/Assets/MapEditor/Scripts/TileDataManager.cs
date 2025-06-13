using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MapEditor
{
    /// <summary>
    /// タイルデータの定義(summer2025_hunt_tile)
    /// </summary>
    [System.Serializable]
    public class TileData
    {
        /// <summary>タイルID</summary>
        public int id;
        /// <summary>グループID</summary>
        public int groupId;
        /// <summary>地形種別</summary>
        public FieldTileType fieldTileType;
        /// <summary>ギミック種別</summary>
        public GimmickTileType gimmickTileType;
        /// <summary>ギミックマスターID</summary>
        public int gimmickTileMasterId = 0;
        /// <summary>移動可能か</summary>
        public bool isMovable = true;
        /// <summary>タイル位置</summary>
        public Vector3Int position;
    }

    /// <summary>
    /// タイルデータ管理ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "TileDataManager", menuName = "TileMap/TileDataManager", order = 2)]
    public class TileDataManager : ScriptableObject
    {
        /// <summary>タイルデータリスト</summary>
        public List<TileData> tilesData = new();
    }
}
