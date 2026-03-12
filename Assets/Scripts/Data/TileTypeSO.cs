using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/TileTypes")]
public class TileTypeSO : ScriptableObject
{
    // The sprite is all GridManager currently needs from a tile type asset.
    public Sprite sprite;
}
