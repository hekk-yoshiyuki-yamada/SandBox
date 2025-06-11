using UnityEngine;

public class MapBoundary : MonoBehaviour, IPlayerMapBoundary
{
    [SerializeField] SpriteRenderer mapSpriteRenderer;
    [SerializeField] float cellSize = 1f;

    private Bounds mapBounds;
    public Bounds MapBounds => mapBounds;

    private void Awake()
    {
        if (mapSpriteRenderer != null)
        {
            mapBounds = mapSpriteRenderer.bounds;
        }
    }

    public bool CanMove(Vector2Int gridPos)
    {
        var worldPos = new Vector2(gridPos.x * cellSize, gridPos.y * cellSize);
        var minX = mapBounds.min.x + cellSize;
        var maxX = mapBounds.max.x - cellSize;
        var minY = mapBounds.min.y + cellSize;
        var maxY = mapBounds.max.y - cellSize;

        Debug.Log($"Checking if can move to {worldPos}: " +
            $"minX={minX}, maxX={maxX}, minY={minY}, maxY={maxY}");

        return worldPos.x >= minX && worldPos.x < maxX &&
               worldPos.y >= minY && worldPos.y < maxY;
    }

    // カメラ移動可能範囲（ワールド座標の矩形）を返す
    public Rect GetCameraMovableRect()
    {
        return new Rect(
            mapBounds.min.x,
            mapBounds.min.y,
            mapBounds.size.x,
            mapBounds.size.y
        );
    }
}