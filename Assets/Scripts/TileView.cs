using UnityEngine;

public class TileView : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;

    public void SetSprite(Sprite sprite)
    {
        spriteRenderer.sprite = sprite;
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
