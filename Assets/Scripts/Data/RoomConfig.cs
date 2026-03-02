using System.Collections.Generic;
using UnityEngine;

public class RoomConfig
{
    public Vector2Int start;
    public Vector2Int exit;
    public Vector2Int reward;

    public HashSet<Vector2Int> lava = new HashSet<Vector2Int>();
    public HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();
}