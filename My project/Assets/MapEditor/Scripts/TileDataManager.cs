using System.Collections.Generic;
using UnityEngine;

namespace MapEditor
{
    [CreateAssetMenu(fileName = "TileDataManager", menuName = "TileMap/TileDataManager", order = 2)]
    public class TileDataManager : ScriptableObject
    {
        public List<TileData> tilesData = new();
    }
}