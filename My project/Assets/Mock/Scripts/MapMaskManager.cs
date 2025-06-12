using System;
using System.Collections.Generic;
using UnityEngine;

public class MapMaskManager : MonoBehaviour
{
    [SerializeField] GameObject maskPrefab;
    [SerializeField] float cellSize = 1f;
    [SerializeField] float offset_x = 0.5f;
    [SerializeField] float offset_y = 0.5f;
    [SerializeField] MapBoundary mapBoundary;

    private Vector2Int mapMin;
    private Vector2Int mapMax;
    private Dictionary<Vector2Int, GameObject> maskDict = new();
    public HashSet<Vector2Int> ClearMaskIds = new();

    public event Action OnMaskChanged;

    void Start()
    {
        var bounds = mapBoundary.MapBounds;
        mapMin = new Vector2Int(
            Mathf.RoundToInt((bounds.min.x + 1) / cellSize),
            Mathf.RoundToInt((bounds.min.y + 1) / cellSize)
        );
        mapMax = new Vector2Int(
            Mathf.RoundToInt((bounds.max.x - 1) / cellSize) - 1,
            Mathf.RoundToInt((bounds.max.y - 1) / cellSize) - 1
        );

        // 未踏破マス設置
        for (var x = mapMin.x; x <= mapMax.x; x++)
        {
            for (var y = mapMin.y; y <= mapMax.y; y++)
            {
                Vector2Int pos = new(x, y);
                var mask = Instantiate(maskPrefab, new Vector3(x * cellSize + offset_x, y * cellSize + offset_y, 0), Quaternion.identity, transform);
                maskDict[pos] = mask;
            }
        }
        OnMaskChanged?.Invoke();
    }

    public void UpdateMaskVisibility(Vector2Int playerPos)
    {
        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                var pos = playerPos + new Vector2Int(dx, dy);
                if (!ClearMaskIds.Contains(pos))
                {
                    ClearMaskIds.Add(pos);
                }
            }
        }
        foreach (var kv in maskDict)
        {
            kv.Value.SetActive(!ClearMaskIds.Contains(kv.Key));
        }
    }
}