using UnityEngine;
using UnityEngine.Serialization;

public class GridManager : MonoBehaviour
{
    public static GridManager I { get; private set; }

    [FormerlySerializedAs("width")]
    [SerializeField] int gridWidth = 10;
    [FormerlySerializedAs("height")]
    [SerializeField] int gridHeight = 10;
    [FormerlySerializedAs("cellSize")]
    [SerializeField] float gridCellSize = 1f;
    [FormerlySerializedAs("tilePrefab")]
    [SerializeField] TileView tileViewPrefab;

    [FormerlySerializedAs("floorType")]
    [SerializeField] TileTypeSO floorTileType;
    [FormerlySerializedAs("lavaType")]
    [SerializeField] TileTypeSO lavaTileType;
    [FormerlySerializedAs("blockedType")]
    [SerializeField] TileTypeSO blockedTileType;
    [FormerlySerializedAs("exitType")]
    [SerializeField] TileTypeSO exitTileType;

    public int Width => gridWidth;
    public int Height => gridHeight;

    TileView[,] _tileViews;
    Vector3 _gridOriginWorldPosition;

    void Awake() => I = this;

    void Start()
    {
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
            tileView.Init(tileGridPosition);
            tileView.transform.position = GridToWorld(tileGridPosition);
            _tileViews[x, y] = tileView;
        }
    }

    public void ApplyConfig(RoomConfig roomConfig)
    {
        for (int y = 0; y < gridHeight; y++)
        for (int x = 0; x < gridWidth; x++)
        {
            Vector2Int tileGridPosition = new Vector2Int(x, y);

            if (tileGridPosition == roomConfig.exitPosition)
                _tileViews[x, y].SetSprite(exitTileType.sprite);
            else if (roomConfig.blockedPositions.Contains(tileGridPosition))
                _tileViews[x, y].SetSprite(blockedTileType.sprite);
            else if (roomConfig.lavaPositions.Contains(tileGridPosition))
                _tileViews[x, y].SetSprite(lavaTileType.sprite);
            else
                _tileViews[x, y].SetSprite(floorTileType.sprite);
        }
    }

    public Vector3 GridToWorld(Vector2Int gridPosition)
    {
        return _gridOriginWorldPosition + new Vector3(
            gridPosition.x * gridCellSize,
            gridPosition.y * gridCellSize,
            0f);
    }

    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
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

        RoomConfig activeRoomConfig = RunManager.I != null ? RunManager.I.CurrentRoomConfig : null;
        return activeRoomConfig == null || !activeRoomConfig.blockedPositions.Contains(gridPosition);
    }
}
