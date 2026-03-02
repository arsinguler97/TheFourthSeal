using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/RoomTemplate")]
public class RoomTemplateSO : ScriptableObject
{
    public int lavaCount = 10;
    public int blockedCount = 10;
}