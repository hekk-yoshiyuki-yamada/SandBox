using UnityEngine;

public class PlayerView : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;

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