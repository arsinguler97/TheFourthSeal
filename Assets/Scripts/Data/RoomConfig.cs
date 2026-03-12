using System.Collections.Generic;
using UnityEngine;

public class RoomConfig
{
    // Key positions used by the room logic and scene transitions.
    public Vector2Int start;
    public Vector2Int exit;
    public Vector2Int reward;

    // Every enemy spawn is reserved before hazards are generated so rooms stay playable.
    public List<Vector2Int> enemySpawns = new List<Vector2Int>();

    // Hazard and obstacle cells generated for this specific room instance.
    public HashSet<Vector2Int> lavaTiles = new HashSet<Vector2Int>();
    public HashSet<Vector2Int> blockedTiles = new HashSet<Vector2Int>();
}
