using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomGenerator : MonoBehaviour
{
    [SerializeField] PlayerController playerControllerPrefab;
    [SerializeField] EnemyUnit enemyUnitPrefab;

    PlayerController _spawnedPlayerController;
    readonly List<EnemyUnit> _spawnedEnemyUnits = new List<EnemyUnit>();

    void Start()
    {
        // GridManager builds its tiles in Start, so room generation waits one frame before using it.
        StartCoroutine(GenerateRoomAfterGridReady());
    }

    IEnumerator GenerateRoomAfterGridReady()
    {
        yield return null;

        // Room generation is driven entirely by the floor selection cached in RunManager.
        RoomTemplateSO selectedTemplate = RunManager.I.SelectedRoomTemplate;
        if (selectedTemplate == null)
            yield break;

        int gridWidth = GridManager.I.GridWidth;
        int gridHeight = GridManager.I.GridHeight;

        RoomConfig generatedRoomConfig = new RoomConfig();

        // Start is always on the bottom row, exit on the top row, reward somewhere in between.
        generatedRoomConfig.start = new Vector2Int(Random.Range(0, gridWidth), 0);
        generatedRoomConfig.exit = new Vector2Int(Random.Range(0, gridWidth), gridHeight - 1);
        generatedRoomConfig.reward = new Vector2Int(Random.Range(0, gridWidth), Random.Range(1, gridHeight - 1));

        SpawnPlayerAtGridPosition(generatedRoomConfig.start);

        HashSet<Vector2Int> reservedPositions = new HashSet<Vector2Int>
        {
            generatedRoomConfig.start,
            generatedRoomConfig.exit,
            generatedRoomConfig.reward
        };

        int enemyCount = RunManager.I != null ? RunManager.I.SelectedRoomEnemyCount : selectedTemplate.enemyCount;
        PlaceEnemySpawns(generatedRoomConfig, enemyCount, reservedPositions, gridWidth, gridHeight);
        PlaceRandomPositions(
            generatedRoomConfig.lavaTiles,
            selectedTemplate.lavaTileCount,
            reservedPositions,
            gridWidth,
            gridHeight);
        reservedPositions.UnionWith(generatedRoomConfig.lavaTiles);
        PlaceRandomPositions(
            generatedRoomConfig.blockedTiles,
            selectedTemplate.blockedTileCount,
            reservedPositions,
            gridWidth,
            gridHeight);
        reservedPositions.UnionWith(generatedRoomConfig.blockedTiles);

        SpawnEnemiesAtGridPositions(generatedRoomConfig.enemySpawns);
        RunManager.I.CurrentRoomConfig = generatedRoomConfig;
        GridManager.I.ApplyConfig(generatedRoomConfig);

        if (TurnManager.I != null)
            TurnManager.I.InitializeTurnOrder();
    }

    // Fills a set with unique random cells, skipping positions already reserved by key room elements.
    void PlaceRandomPositions(
        HashSet<Vector2Int> targetPositions,
        int desiredCount,
        HashSet<Vector2Int> reservedPositions,
        int gridWidth,
        int gridHeight)
    {
        int attemptCount = 0;

        while (targetPositions.Count < desiredCount && attemptCount < 5000)
        {
            attemptCount++;
            Vector2Int randomGridPosition = new Vector2Int(Random.Range(0, gridWidth), Random.Range(0, gridHeight));
            if (reservedPositions.Contains(randomGridPosition)) continue;
            targetPositions.Add(randomGridPosition);
        }
    }

    void SpawnPlayerAtGridPosition(Vector2Int spawnGridPosition)
    {
        // Reuse an existing player if one is already in the scene; otherwise instantiate the assigned prefab.
        if (_spawnedPlayerController == null)
        {
            _spawnedPlayerController = FindFirstObjectByType<PlayerController>();

            if (_spawnedPlayerController == null && playerControllerPrefab != null)
                _spawnedPlayerController = Instantiate(playerControllerPrefab);
        }

        if (_spawnedPlayerController != null)
            _spawnedPlayerController.SetGridPosition(spawnGridPosition);
    }

    void SpawnEnemiesAtGridPositions(List<Vector2Int> spawnGridPositions)
    {
        // Existing enemies are reused when possible so repeated room loads do not instantiate endlessly.
        while (_spawnedEnemyUnits.Count < spawnGridPositions.Count)
        {
            EnemyUnit enemyUnit = FindFirstObjectByType<EnemyUnit>();

            if (enemyUnit != null && !_spawnedEnemyUnits.Contains(enemyUnit))
            {
                _spawnedEnemyUnits.Add(enemyUnit);
                continue;
            }

            if (enemyUnitPrefab == null)
                break;

            _spawnedEnemyUnits.Add(Instantiate(enemyUnitPrefab));
        }

        for (int i = 0; i < _spawnedEnemyUnits.Count; i++)
        {
            bool shouldBeActive = i < spawnGridPositions.Count;
            _spawnedEnemyUnits[i].gameObject.SetActive(shouldBeActive);

            if (shouldBeActive)
                _spawnedEnemyUnits[i].SetGridPosition(spawnGridPositions[i]);
        }
    }

    void PlaceEnemySpawns(RoomConfig roomConfig, int enemyCount, HashSet<Vector2Int> reservedPositions, int gridWidth, int gridHeight)
    {
        roomConfig.enemySpawns.Clear();

        // Enemy spawns are reserved before hazards so combat spaces stay playable.
        for (int enemyIndex = 0; enemyIndex < enemyCount; enemyIndex++)
        {
            Vector2Int enemySpawnGridPosition = FindRandomEnemySpawnPosition(
                reservedPositions,
                roomConfig.start,
                gridWidth,
                gridHeight);
            roomConfig.enemySpawns.Add(enemySpawnGridPosition);
            reservedPositions.Add(enemySpawnGridPosition);
        }
    }

    Vector2Int FindRandomEnemySpawnPosition(
        HashSet<Vector2Int> reservedPositions,
        Vector2Int playerStartGridPosition,
        int gridWidth,
        int gridHeight)
    {
        int attemptCount = 0;

        while (attemptCount < 5000)
        {
            attemptCount++;
            Vector2Int randomGridPosition = new Vector2Int(Random.Range(0, gridWidth), Random.Range(0, gridHeight));
            if (reservedPositions.Contains(randomGridPosition))
                continue;

            int distanceFromPlayerStart = Mathf.Abs(randomGridPosition.x - playerStartGridPosition.x)
                + Mathf.Abs(randomGridPosition.y - playerStartGridPosition.y);
            if (distanceFromPlayerStart <= 2)
                continue;

            return randomGridPosition;
        }

        return FindRandomFreePosition(reservedPositions, gridWidth, gridHeight);
    }

    Vector2Int FindRandomFreePosition(HashSet<Vector2Int> reservedPositions, int gridWidth, int gridHeight)
    {
        int attemptCount = 0;

        while (attemptCount < 5000)
        {
            attemptCount++;
            Vector2Int randomGridPosition = new Vector2Int(Random.Range(0, gridWidth), Random.Range(0, gridHeight));
            if (reservedPositions.Contains(randomGridPosition))
                continue;

            return randomGridPosition;
        }

        return new Vector2Int(0, 0);
    }
}
