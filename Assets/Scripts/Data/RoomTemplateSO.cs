using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "ScriptableObjects/RoomTemplate")]
public class RoomTemplateSO : ScriptableObject
{
    // How many enemies should be spawned into the room before hazards are placed.
    public int enemyCount = 1;

    // If assigned, this room will always spawn exactly one copy of this enemy, then fill any
    // remaining enemy slots with normal enemies from the template or node override pool.
    public EnemyDefinitionSO bossEnemy;

    // How many lava tiles should be added when this template is used.
    public int lavaTileCount = 10;

    // How many blocked tiles should be added when this template is used.
    public int blockedTileCount = 10;

    // How many lightning tiles should be added when this template is used.
    public int lightningTileCount = 10;

    // If populated, enemies for this room are chosen from this list.
    public List<EnemyDefinitionSO> possibleEnemies = new List<EnemyDefinitionSO>();
}
