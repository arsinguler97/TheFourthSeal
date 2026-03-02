using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager I { get; private set; }

    [SerializeField] int width = 10;
    [SerializeField] int height = 10;
    [SerializeField] float cellSize = 1f;
    [SerializeField] TileView tilePrefab;

    [SerializeField] TileTypeSO floorType;
    [SerializeField] TileTypeSO lavaType;
    [SerializeField] TileTypeSO blockedType;
    [SerializeField] TileTypeSO exitType;

    public int Width => width;
    public int Height => height;

    TileView[,] _tiles;
    Vector3 _origin;

    void Awake() => I = this;

    void Start()
    {
        _origin = new Vector3(-((width - 1) * cellSize) * 0.5f,
                              -((height - 1) * cellSize) * 0.5f,
                              0f);

        Build();
    }

    void Build()
    {
        _tiles = new TileView[width, height];

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            var t = Instantiate(tilePrefab, transform);
            t.Init(new Vector2Int(x, y));
            t.transform.position = GridToWorld(new Vector2Int(x, y));
            _tiles[x, y] = t;
        }
    }

    public void ApplyConfig(RoomConfig cfg)
    {
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            var p = new Vector2Int(x, y);

            if (p == cfg.exit)
                _tiles[x, y].SetSprite(exitType.sprite);
            else if (cfg.blocked.Contains(p))
                _tiles[x, y].SetSprite(blockedType.sprite);
            else if (cfg.lava.Contains(p))
                _tiles[x, y].SetSprite(lavaType.sprite);
            else
                _tiles[x, y].SetSprite(floorType.sprite);
        }
    }

    public Vector3 GridToWorld(Vector2Int g)
    {
        return _origin + new Vector3(g.x * cellSize, g.y * cellSize, 0f);
    }

    public Vector2Int WorldToGrid(Vector3 w)
    {
        var p = w - _origin;
        int x = Mathf.RoundToInt(p.x / cellSize);
        int y = Mathf.RoundToInt(p.y / cellSize);
        return new Vector2Int(x, y);
    }

    public bool InBounds(Vector2Int g)
    {
        return g.x >= 0 && g.y >= 0 && g.x < width && g.y < height;
    }

    public bool IsWalkable(Vector2Int g)
    {
        if (!InBounds(g))
            return false;

        var cfg = RunManager.I != null ? RunManager.I.CurrentRoomConfig : null;
        return cfg == null || !cfg.blocked.Contains(g);
    }
}
