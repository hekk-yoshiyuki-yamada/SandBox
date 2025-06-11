using UnityEngine;

public class PlayerPresenter
{
    private PlayerView view;
    private Vector2Int position;
    private MapBoundary mapBoundary;
    private MapMaskManager mapMaskManager;

    public PlayerPresenter(PlayerView view, Vector2Int startGridPos, MapBoundary mapBoundary, MapMaskManager mapMaskManager)
    {
        this.view = view;
        this.position = startGridPos;
        this.mapBoundary = mapBoundary;
        this.mapMaskManager = mapMaskManager;
        view.SetPosition(position);
        mapMaskManager?.UpdateMaskVisibility(position); // 起動直後に実行
    }

    public void Move(Vector2Int dir)
    {
        var next = position + dir;
        if (mapBoundary.CanMove(next))
        {
            position = next;
            view.SetPosition(position);
            mapMaskManager?.UpdateMaskVisibility(position); // 移動後に実行
        }
    }
}