using UnityEngine;

public class TileView : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;

    public void SetSprite(Sprite sprite)
    {
        spriteRenderer.sprite = sprite;
    }

    public void SetAlpha(float alpha)
    {
        Color color = spriteRenderer.color;
        color.a = Mathf.Clamp01(alpha);
        spriteRenderer.color = color;
    }

    public void SetRenderOrder(int renderOrder)
    {
        spriteRenderer.sortingOrder = renderOrder;
    }

    public Sprite GetSprite()
    {
        return spriteRenderer.sprite;
    }
}
