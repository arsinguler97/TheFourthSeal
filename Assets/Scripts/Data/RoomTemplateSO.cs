using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "ScriptableObjects/RoomTemplate")]
public class RoomTemplateSO : ScriptableObject
{
    [FormerlySerializedAs("lavaCount")]
    public int lavaTileCount = 10;
    [FormerlySerializedAs("blockedCount")]
    public int blockedTileCount = 10;
}
