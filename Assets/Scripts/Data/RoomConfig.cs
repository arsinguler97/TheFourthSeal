using System.Collections.Generic;
using UnityEngine;

public class RoomConfig
{
    public Vector2Int startPosition;
    public Vector2Int exitPosition;
    public Vector2Int rewardPosition;

    public HashSet<Vector2Int> lavaPositions = new HashSet<Vector2Int>();
    public HashSet<Vector2Int> blockedPositions = new HashSet<Vector2Int>();

    public Vector2Int start
    {
        get => startPosition;
        set => startPosition = value;
    }

    public Vector2Int exit
    {
        get => exitPosition;
        set => exitPosition = value;
    }

    public Vector2Int reward
    {
        get => rewardPosition;
        set => rewardPosition = value;
    }

    public HashSet<Vector2Int> lava
    {
        get => lavaPositions;
        set => lavaPositions = value;
    }

    public HashSet<Vector2Int> blocked
    {
        get => blockedPositions;
        set => blockedPositions = value;
    }
}
