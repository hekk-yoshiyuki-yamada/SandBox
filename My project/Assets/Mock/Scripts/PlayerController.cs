using UnityEngine;

public class PlayerController
{
    private PlayerPresenter presenter;

    private Vector2Int holdDir = Vector2Int.zero;
    private float keyDownTime = 0f;
    private float lastMoveTime = 0f;
    private bool isHolding = false;

    private float holdThreshold = 0.1f;
    private float longPressThreshold = 0.2f;
    private float fastLongPressThreshold = 2f;
    private float longPressInterval = 4f;
    private float fastLongPressInterval = 8f;

    public PlayerController(PlayerPresenter presenter)
    {
        this.presenter = presenter;
    }

    public void Update()
    {
        var dir = Vector2Int.zero;
        if (Input.GetKey(KeyCode.LeftArrow)) dir = Vector2Int.left;
        if (Input.GetKey(KeyCode.RightArrow)) dir = Vector2Int.right;
        if (Input.GetKey(KeyCode.UpArrow)) dir = Vector2Int.up;
        if (Input.GetKey(KeyCode.DownArrow)) dir = Vector2Int.down;

        if (dir != Vector2Int.zero && holdDir == Vector2Int.zero)
        {
            holdDir = dir;
            keyDownTime = Time.time;
            lastMoveTime = 0f;
            isHolding = false;
            presenter.Move(holdDir);
        }
        else if (dir == holdDir && holdDir != Vector2Int.zero)
        {
            var held = Time.time - keyDownTime;
            if (!isHolding)
            {
                if (held >= holdThreshold)
                {
                    isHolding = true;
                    lastMoveTime = Time.time;
                }
            }
            else
            {
                var interval = 1f / (held >= fastLongPressThreshold ? fastLongPressInterval : (held >= longPressThreshold ? longPressInterval : 1f));
                if (Time.time - lastMoveTime >= interval)
                {
                    presenter.Move(holdDir);
                    lastMoveTime = Time.time;
                }
            }
        }
        else if (dir == Vector2Int.zero)
        {
            holdDir = Vector2Int.zero;
            isHolding = false;
        }
    }
}