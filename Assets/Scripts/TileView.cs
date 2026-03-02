using UnityEngine;

public class TileView : MonoBehaviour
{
    [SerializeField] SpriteRenderer sr;

    public Vector2Int GridPos { get; private set; }

    public void Init(Vector2Int gridPos)
    {
        GridPos = gridPos;
    }

    public void SetSprite(Sprite sprite)
    {
        sr.sprite = sprite;
    }
}