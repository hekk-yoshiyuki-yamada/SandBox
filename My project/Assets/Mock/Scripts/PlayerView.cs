using UnityEngine;

public class PlayerView : MonoBehaviour
{
    [SerializeField] Vector2Int startGridPos;
    [SerializeField] MapBoundary mapBoundary;
    [SerializeField] MapMaskManager mapMaskManager;
    [SerializeField] SpriteRenderer spriteRenderer;

    private PlayerPresenter presenter;
    private PlayerController controller;

    void Start()
    {
        presenter = new PlayerPresenter(this, startGridPos, mapBoundary, mapMaskManager);
        controller = new PlayerController(presenter);
    }

    void Update()
    {
        controller.Update();
    }

    public void SetPosition(Vector2Int gridPos)
    {
        transform.position = new Vector3(gridPos.x, gridPos.y, 0);
    }

    public void SetFlip(bool flip)
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = flip;
    }

    public void SetScale(float scale)
    {
        if (spriteRenderer != null)
            spriteRenderer.transform.localScale = new Vector3(scale, scale);
    }

}