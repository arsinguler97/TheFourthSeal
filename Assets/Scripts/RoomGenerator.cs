using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class RoomGenerator : MonoBehaviour
{
    [FormerlySerializedAs("playerPrefab")]
    [SerializeField] PlayerController playerControllerPrefab;

    PlayerController _spawnedPlayerController;

    void Start()
    {
        StartCoroutine(GenerateRoomAfterGridReady());
    }

    IEnumerator GenerateRoomAfterGridReady()
    {
        yield return null;

        RoomTemplateSO selectedTemplate = RunManager.I.SelectedRoomTemplate;
        if (selectedTemplate == null) yield break;

        int gridWidth = GridManager.I.Width;
        int gridHeight = GridManager.I.Height;

        RoomConfig generatedRoomConfig = new RoomConfig();

        generatedRoomConfig.startPosition = new Vector2Int(Random.Range(0, gridWidth), 0);
        generatedRoomConfig.exitPosition = new Vector2Int(Random.Range(0, gridWidth), gridHeight - 1);
        generatedRoomConfig.rewardPosition = new Vector2Int(Random.Range(0, gridWidth), Random.Range(1, gridHeight - 1));

        SpawnPlayerAtGridPosition(generatedRoomConfig.startPosition);

        HashSet<Vector2Int> reservedPositions = new HashSet<Vector2Int>
        {
            generatedRoomConfig.startPosition,
            generatedRoomConfig.exitPosition,
            generatedRoomConfig.rewardPosition
        };

        PlaceRandomPositions(
            generatedRoomConfig.lavaPositions,
            selectedTemplate.lavaTileCount,
            reservedPositions,
            gridWidth,
            gridHeight);
        reservedPositions.UnionWith(generatedRoomConfig.lavaPositions);
        PlaceRandomPositions(
            generatedRoomConfig.blockedPositions,
            selectedTemplate.blockedTileCount,
            reservedPositions,
            gridWidth,
            gridHeight);

        RunManager.I.CurrentRoomConfig = generatedRoomConfig;

        GridManager.I.ApplyConfig(generatedRoomConfig);
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
        if (_spawnedPlayerController == null)
        {
            _spawnedPlayerController = FindFirstObjectByType<PlayerController>();

            if (_spawnedPlayerController == null && playerControllerPrefab != null)
                _spawnedPlayerController = Instantiate(playerControllerPrefab);
        }

        if (_spawnedPlayerController != null)
            _spawnedPlayerController.SetGridPosition(spawnGridPosition);
    }
}
