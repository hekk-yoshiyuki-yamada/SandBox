using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] PlayerView playerView;
    [SerializeField] Vector2Int startGridPos;
    [SerializeField] MapBoundary mapBoundary;
    [SerializeField] float holdThreshold = 0.1f;
    [SerializeField] float longPressThreshold = 0.2f;
    [SerializeField] float fastLongPressThreshold = 2f;
    [SerializeField] float longPressInterval = 4f;
    [SerializeField] float fastLongPressInterval = 8f;

    private PlayerPresenter presenter;

    private Vector2Int holdDir = Vector2Int.zero;
    private float keyDownTime = 0f;
    private float lastMoveTime = 0f;
    private bool isHolding = false;

    void Start()
    {
        presenter = new PlayerPresenter(playerView, startGridPos, mapBoundary);
    }

    void Update()
    {
        // 入力取得
        Vector2Int dir = Vector2Int.zero;
        if (Input.GetKey(KeyCode.LeftArrow)) dir = Vector2Int.left;
        if (Input.GetKey(KeyCode.RightArrow)) dir = Vector2Int.right;
        if (Input.GetKey(KeyCode.UpArrow)) dir = Vector2Int.up;
        if (Input.GetKey(KeyCode.DownArrow)) dir = Vector2Int.down;

        // 新規押下
        if (dir != Vector2Int.zero && holdDir == Vector2Int.zero)
        {
            holdDir = dir;
            keyDownTime = Time.time;
            lastMoveTime = 0f;
            isHolding = false;
            presenter.Move(holdDir); // 単発押しで1マスだけ進む
        }
        // 長押し判定
        else if (dir == holdDir && holdDir != Vector2Int.zero)
        {
            float held = Time.time - keyDownTime;
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
                float interval = 1f / (held >= fastLongPressThreshold ? fastLongPressInterval : (held >= longPressThreshold ? longPressInterval: 1f));
                if (Time.time - lastMoveTime >= interval)
                {
                    presenter.Move(holdDir);
                    lastMoveTime = Time.time;
                }
            }
        }
        // 離した
        else if (dir == Vector2Int.zero)
        {
            holdDir = Vector2Int.zero;
            isHolding = false;
        }
    }

    public Vector2Int GetPlayerGridPos() => presenter.GetPosition();
}