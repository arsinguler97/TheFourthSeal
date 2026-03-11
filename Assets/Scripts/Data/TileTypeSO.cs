using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "ScriptableObjects/TileTypes")]
public class TileTypeSO : ScriptableObject
{
    [FormerlySerializedAs("id")]
    public string tileId;
    [FormerlySerializedAs("walkable")]
    public bool isWalkable = true;
    [FormerlySerializedAs("allowSpawn")]
    public bool canBeUsedForSpawn = true;
    public Sprite sprite;
}
