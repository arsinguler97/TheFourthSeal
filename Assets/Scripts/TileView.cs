using UnityEngine;
using UnityEngine.Serialization;

public class TileView : MonoBehaviour
{
    [FormerlySerializedAs("sr")]
    [SerializeField] SpriteRenderer spriteRenderer;

    public Vector2Int GridPos { get; private set; }

    public void Init(Vector2Int gridPos)
    {
        GridPos = gridPos;
    }

    public void SetSprite(Sprite sprite)
    {
        spriteRenderer.sprite = sprite;
    }
}
