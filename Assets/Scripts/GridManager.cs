using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager I { get; private set; }

    [SerializeField] int gridWidth = 10;
    [SerializeField] int gridHeight = 10;
    [SerializeField] float gridCellSize = 1f;
    [SerializeField] TileView tileViewPrefab;

    [SerializeField] TileTypeSO floorTileType;
    [SerializeField] TileTypeSO lavaTileType;
    [SerializeField] TileTypeSO blockedTileType;
    [SerializeField] TileTypeSO exitTileType;
    [SerializeField] TileTypeSO rewardTileType;
    [SerializeField] TileTypeSO rewardOpenedTileType;

    [SerializeField] TileTypeSO walkTileType;
    [SerializeField] TileTypeSO attackTileType;


    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;

    TileView[,] _tileViews;
    TileView[,] _tileViewsWalkGrid;
    TileView[,] _tileViewsAttackGrid;

    Vector3 _gridOriginWorldPosition;





    void Awake() => I = this;

    void OnDestroy()
    {
        if (I == this)
            I = null;
    }

    void Start()
    {
        // The origin is shifted so the grid is centered around the GameObject instead of starting at (0, 0).
        _gridOriginWorldPosition = new Vector3(
            -((gridWidth - 1) * gridCellSize) * 0.5f,
            -((gridHeight - 1) * gridCellSize) * 0.5f,
            0f);

        BuildGrid();
    }

    // Creates one TileView object for each logical grid cell.
    void BuildGrid()
    {
        _tileViews = new TileView[gridWidth, gridHeight];
        _tileViewsWalkGrid = new TileView[gridWidth, gridHeight];
        _tileViewsAttackGrid = new TileView[gridWidth, gridHeight];


        for (int y = 0; y < gridHeight; y++)
        for (int x = 0; x < gridWidth; x++)
        {
            Vector2Int tileGridPosition = new Vector2Int(x, y);
                
            _tileViews[x, y] = CreateTileView(tileGridPosition);
            _tileViews[x, y].SetRenderOrder(1);
            _tileViewsWalkGrid[x, y] = CreateTileView(tileGridPosition);
            _tileViewsWalkGrid[x, y].SetRenderOrder(2);
            _tileViewsAttackGrid[x, y] = CreateTileView(tileGridPosition);
            _tileViewsAttackGrid[x, y].SetRenderOrder(2);
            }
    }

    
    private TileView CreateTileView(Vector2Int tileGridPos)
    {
        TileView tileView = Instantiate(tileViewPrefab, transform);
        tileView.transform.position = GridToWorld(tileGridPos);
        return tileView;
    }



    public void ApplyConfig(RoomConfig roomConfig)
    {
        // Paint each spawned tile according to the generated room data.
        for (int y = 0; y < gridHeight; y++)
        for (int x = 0; x < gridWidth; x++)
        {
            Vector2Int tileGridPosition = new Vector2Int(x, y);

            if (tileGridPosition == roomConfig.exit)
                _tileViews[x, y].SetSprite(exitTileType.sprite);
            else if (tileGridPosition == roomConfig.reward)
                _tileViews[x, y].SetSprite(GetRewardSprite(roomConfig));
            else if (roomConfig.blockedTiles.Contains(tileGridPosition))
                _tileViews[x, y].SetSprite(blockedTileType.sprite);
            else if (roomConfig.lavaTiles.Contains(tileGridPosition))
                _tileViews[x, y].SetSprite(lavaTileType.sprite);
            else
                _tileViews[x, y].SetSprite(floorTileType.sprite);


            _tileViewsWalkGrid[x, y].SetSprite(null);
            _tileViewsAttackGrid[x, y].SetSprite(null);
        }
    }

    public void RefreshRewardTile(RoomConfig roomConfig)
    {
        if (roomConfig == null || !InBounds(roomConfig.reward))
            return;

        _tileViews[roomConfig.reward.x, roomConfig.reward.y].SetSprite(GetRewardSprite(roomConfig));
    }

    public Vector3 GridToWorld(Vector2Int gridPosition)
    {
        // Converts a logical cell index into the centered world position used by sprites and the player.
        return _gridOriginWorldPosition + new Vector3(
            gridPosition.x * gridCellSize,
            gridPosition.y * gridCellSize,
            0f);
    }

    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        // Mouse clicks happen in world space, so they must be mapped back onto the nearest grid cell.
        Vector3 offsetFromOrigin = worldPosition - _gridOriginWorldPosition;
        int x = Mathf.RoundToInt(offsetFromOrigin.x / gridCellSize);
        int y = Mathf.RoundToInt(offsetFromOrigin.y / gridCellSize);
        return new Vector2Int(x, y);
    }

    public bool InBounds(Vector2Int gridPosition)
    {
        return gridPosition.x >= 0
            && gridPosition.y >= 0
            && gridPosition.x < gridWidth
            && gridPosition.y < gridHeight;
    }

    public bool IsWalkable(Vector2Int gridPosition)
    {
        if (!InBounds(gridPosition))
            return false;

        // Only blocked tiles prevent movement; lava is still treated as a valid floor tile here.
        RoomConfig activeRoomConfig = RunManager.I != null ? RunManager.I.CurrentRoomConfig : null;
        return activeRoomConfig == null || !activeRoomConfig.blockedTiles.Contains(gridPosition);
    }

    public bool IsLavaTile(Vector2Int gridPosition)
    {
        if (!InBounds(gridPosition))
            return false;

        RoomConfig activeRoomConfig = RunManager.I != null ? RunManager.I.CurrentRoomConfig : null;
        return activeRoomConfig != null && activeRoomConfig.lavaTiles.Contains(gridPosition);
    }




    public void SetWalkGrids(Vector2Int unitGridPosition, int unitWalkDistance, int unitAttackRange)
    {
        // Uses Breadth-First Search https://www.geeksforgeeks.org/dsa/breadth-first-search-or-bfs-for-a-graph/
        // Loops over the grid from the center point outwards (Player pos is center)
        Queue<(Vector2Int pos, int cost)> tilesQueued = new();
        HashSet<Vector2Int> tilesVisited = new();

        tilesQueued.Enqueue((unitGridPosition, 0));
        tilesVisited.Add(unitGridPosition);

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        while (tilesQueued.Count > 0)
        {
            var (pos, cost) = tilesQueued.Dequeue();

            if (cost <= unitWalkDistance)
            {
                _tileViewsWalkGrid[pos.x, pos.y].SetSprite(walkTileType.sprite);
            }
            else if (cost <= unitWalkDistance + unitAttackRange)
            {
                _tileViewsWalkGrid[pos.x, pos.y].SetSprite(attackTileType.sprite);
            }
            else
                continue;


            if (cost >= unitWalkDistance + unitAttackRange)
                continue;


            foreach (Vector2Int dir in directions)
            {
                Vector2Int next = pos + dir;

                if (next.x < 0 || next.x >= gridWidth || next.y < 0 || next.y >= gridHeight)
                    continue;

                if (tilesVisited.Contains(next))
                    continue;

                if (_tileViews[next.x, next.y].GetSprite() == blockedTileType.sprite)
                    continue;

                tilesVisited.Add(next);
                tilesQueued.Enqueue((next, cost + 1));
            }
        }

        /* Old Version - Loops over the Grid from corner to corner
        for (int x = -unitWalkDistance - unitAttackRange; x <= unitWalkDistance + unitAttackRange; x++)
        {
            for (int y = -unitWalkDistance - unitAttackRange; y <= unitWalkDistance + unitAttackRange; y++)
            {
                int tileX = x + unitGridPosition.x;
                int tileY = y + unitGridPosition.y;

                if (!((tileX >= 0 && tileX < gridWidth) && (tileY >= 0 && tileY < gridHeight))) continue;

                if (_tileViews[tileX, tileY].GetSprite() == blockedTileType.sprite) continue;

                if (Mathf.Abs(x) + Mathf.Abs(y) <= unitWalkDistance)
                {
                    _tileViewsWalkGrid[tileX, tileY].SetSprite(walkTileType.sprite);
                }
                else if (Mathf.Abs(x) + Mathf.Abs(y) <= unitWalkDistance + unitAttackRange)
                {
                    _tileViewsWalkGrid[tileX, tileY].SetSprite(attackTileType.sprite);
                }
            }
        }*/
    }

    public void SetAttackGrids(Vector2Int unitGridPosition, int unitAttackRange)
    {
        for (int x = -unitAttackRange; x <= unitAttackRange; x++)
        {
            for (int y = -unitAttackRange; y <= unitAttackRange; y++)
            {
                int tileX = x + unitGridPosition.x;
                int tileY = y + unitGridPosition.y;

                if (!((tileX >= 0 && tileX < gridWidth) && (tileY >= 0 && tileY < gridHeight))) continue;

                if (_tileViews[tileX, tileY].GetSprite() == blockedTileType.sprite) continue;

                if (Mathf.Abs(x) + Mathf.Abs(y) == unitAttackRange)
                {
                    _tileViewsAttackGrid[tileX, tileY].SetSprite(attackTileType.sprite);
                }
            }
        }
    }

    public void ResetWalkGrids()
    {
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                _tileViewsWalkGrid[x,y].SetSprite(null);
    }

    public void ResetAttackGrids()
    {
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                _tileViewsAttackGrid[x, y].SetSprite(null);
    }

    Sprite GetRewardSprite(RoomConfig roomConfig)
    {
        if (roomConfig != null && roomConfig.isRewardOpened && rewardOpenedTileType != null)
            return rewardOpenedTileType.sprite;

        return rewardTileType != null ? rewardTileType.sprite : floorTileType.sprite;
    }
}
