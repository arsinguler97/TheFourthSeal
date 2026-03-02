using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomGenerator : MonoBehaviour
{
    [SerializeField] PlayerController playerPrefab;

    PlayerController _spawnedPlayer;

    void Start()
    {
        StartCoroutine(GenerateAfterGridReady());
    }

    IEnumerator GenerateAfterGridReady()
    {
        yield return null;

        var template = RunManager.I.SelectedRoomTemplate;
        if (template == null) yield break;

        int width = GridManager.I.Width;
        int height = GridManager.I.Height;

        var cfg = new RoomConfig();

        cfg.start = new Vector2Int(Random.Range(0, width), 0);
        cfg.exit = new Vector2Int(Random.Range(0, width), height - 1);
        cfg.reward = new Vector2Int(Random.Range(0, width), Random.Range(1, height - 1));

        SpawnPlayer(cfg.start);

        var reserved = new HashSet<Vector2Int> { cfg.start, cfg.exit, cfg.reward };

        PlaceRandom(cfg.lava, template.lavaCount, reserved, width, height);
        reserved.UnionWith(cfg.lava);
        PlaceRandom(cfg.blocked, template.blockedCount, reserved, width, height);

        RunManager.I.CurrentRoomConfig = cfg;

        GridManager.I.ApplyConfig(cfg);
    }

    void PlaceRandom(HashSet<Vector2Int> set, int count, HashSet<Vector2Int> reserved, int width, int height)
    {
        int tries = 0;

        while (set.Count < count && tries < 5000)
        {
            tries++;
            var p = new Vector2Int(Random.Range(0, width), Random.Range(0, height));
            if (reserved.Contains(p)) continue;
            set.Add(p);
        }
    }

    void SpawnPlayer(Vector2Int spawnGrid)
    {
        if (_spawnedPlayer == null)
        {
            _spawnedPlayer = FindFirstObjectByType<PlayerController>();

            if (_spawnedPlayer == null && playerPrefab != null)
                _spawnedPlayer = Instantiate(playerPrefab);
        }

        if (_spawnedPlayer != null)
            _spawnedPlayer.SetGridPosition(spawnGrid);
    }
}
