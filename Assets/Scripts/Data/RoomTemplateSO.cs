using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/RoomTemplate")]
public class RoomTemplateSO : ScriptableObject
{
    // How many enemies should be spawned into the room before hazards are placed.
    public int enemyCount = 1;

    // How many lava tiles should be added when this template is used.
    public int lavaTileCount = 10;

    // How many blocked tiles should be added when this template is used.
    public int blockedTileCount = 10;
}
