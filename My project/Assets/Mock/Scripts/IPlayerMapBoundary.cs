using UnityEngine;

public interface IPlayerMapBoundary
{
    bool CanMove(Vector2Int gridPos);
}