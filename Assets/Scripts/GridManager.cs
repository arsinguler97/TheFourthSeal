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

    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;

    TileView[,] _tileViews;
    Vector3 _gridOriginWorldPosition;

    void Awake() => I = this;

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

        for (int y = 0; y < gridHeight; y++)
        for (int x = 0; x < gridWidth; x++)
        {
            TileView tileView = Instantiate(tileViewPrefab, transform);
            Vector2Int tileGridPosition = new Vector2Int(x, y);
            tileView.transform.position = GridToWorld(tileGridPosition);
            _tileViews[x, y] = tileView;
        }
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
            else if (roomConfig.blockedTiles.Contains(tileGridPosition))
                _tileViews[x, y].SetSprite(blockedTileType.sprite);
            else if (roomConfig.lavaTiles.Contains(tileGridPosition))
                _tileViews[x, y].SetSprite(lavaTileType.sprite);
            else
                _tileViews[x, y].SetSprite(floorTileType.sprite);
        }
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
}
