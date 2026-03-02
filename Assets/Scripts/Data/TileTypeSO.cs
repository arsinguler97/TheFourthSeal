using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/TileTypes")]
public class TileTypeSO : ScriptableObject
{
    public string id;
    public bool walkable = true;
    public bool allowSpawn = true;
    public Sprite sprite;
}