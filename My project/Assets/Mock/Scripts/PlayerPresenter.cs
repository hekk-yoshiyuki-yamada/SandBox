// PlayerPresenter.cs
using UnityEngine;

public class PlayerPresenter
{
    private PlayerView view;
    private Vector2Int position;
    private readonly IPlayerMapBoundary boundary;

    public PlayerPresenter(PlayerView view, Vector2Int startPos, IPlayerMapBoundary boundary)
    {
        this.view = view;
        this.position = startPos;
        this.boundary = boundary;
        view.SetPosition(position);
    }

    public void Move(Vector2Int dir)
    {
        Vector2Int next = position + dir;
        if (boundary.CanMove(next))
        {
            position = next;
            view.SetPosition(position);
            if (dir.x != 0)
                view.SetFlip(dir.x > 0);
        }
    }

    public Vector2Int GetPosition() => position;
}