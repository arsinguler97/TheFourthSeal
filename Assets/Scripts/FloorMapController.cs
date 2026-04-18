using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FloorMapController : MonoBehaviour
{
    public static FloorMapController I { get; private set; }

    // These references are scene-local and should belong to FloorScene, not the persistent RunManager object.
    [SerializeField] FloorMapPlayerUI floorMapPlayerUI;
    [SerializeField] RoomNode startingRoomNode;
    [SerializeField] List<RoomNode> roomNodes = new List<RoomNode>();
    [SerializeField] Graphic floorKeyIndicator;
    bool _isTransitioningToRoom;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
    }

    void OnDestroy()
    {
        if (I == this)
            I = null;
    }

    void Start()
    {
        if (RunManager.I == null)
            return;

        // Rebuild interactable state from persistent run progress every time FloorScene loads.
        RefreshNodeStates();

        RoomNode currentNode = GetNodeById(RunManager.I.CurrentFloorNodeId);
        if (currentNode == null)
            currentNode = startingRoomNode;

        if (floorMapPlayerUI != null && currentNode != null)
            floorMapPlayerUI.SnapTo(currentNode.GetComponent<RectTransform>());

        RefreshKeyIndicator();
    }

    public void SelectRoomNode(RoomNode roomNode, RoomTemplateSO roomTemplate, int enemyCountOverride)
    {
        if (_isTransitioningToRoom || roomNode == null || RunManager.I == null)
            return;

        if (string.IsNullOrWhiteSpace(roomNode.NodeId))
        {
            Debug.LogWarning($"SelectRoomNode aborted because '{roomNode.name}' has an empty NodeId.");
            return;
        }

        if (RunManager.I.IsFloorNodeCleared(roomNode.NodeId))
        {
            NavigateToClearedRoomNode(roomNode);
            return;
        }

        if (roomNode.LoadsSceneDirectly)
        {
            if (!RunManager.I.HasFloorKey)
            {
                Debug.Log($"Scene exit '{roomNode.NodeId}' is locked until the floor key is collected.");
                return;
            }

            BeginSceneTransition(roomNode, roomNode.DestinationSceneName);
            return;
        }

        if (roomTemplate == null)
        {
            Debug.LogWarning($"SelectRoomNode aborted because '{roomNode.name}' has no RoomTemplateSO assigned.");
            return;
        }

        RunManager.I.PrepareRoomSelection(roomNode.NodeId, roomTemplate, enemyCountOverride, roomNode.EnemyOverrides);
        BeginSceneTransition(roomNode, "RoomScene");
    }

    void BeginSceneTransition(RoomNode roomNode, string sceneName)
    {
        if (roomNode == null || string.IsNullOrWhiteSpace(sceneName))
            return;

        _isTransitioningToRoom = true;

        // The map marker animation is cosmetic; the room load still works without it.
        RectTransform targetRect = roomNode.GetComponent<RectTransform>();
        if (floorMapPlayerUI != null && targetRect != null)
        {
            floorMapPlayerUI.MoveTo(targetRect, () => SceneManager.LoadScene(sceneName));
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    void NavigateToClearedRoomNode(RoomNode roomNode)
    {
        if (roomNode == null || RunManager.I == null)
            return;

        void CompleteNavigation()
        {
            RunManager.I.SetCurrentFloorNodePosition(roomNode.NodeId);
            RefreshNodeStates();
        }

        RectTransform targetRect = roomNode.GetComponent<RectTransform>();
        if (floorMapPlayerUI != null && targetRect != null)
        {
            floorMapPlayerUI.MoveTo(targetRect, CompleteNavigation);
            return;
        }

        CompleteNavigation();
    }

    void RefreshNodeStates()
    {
        // Only rooms connected to the current floor node should be selectable.
        RoomNode currentNode = GetNodeById(RunManager.I.CurrentFloorNodeId);
        if (currentNode == null)
            currentNode = startingRoomNode;

        for (int i = 0; i < roomNodes.Count; i++)
        {
            RoomNode roomNode = roomNodes[i];
            if (roomNode == null || roomNode.RoomButton == null)
                continue;

            bool isCleared = RunManager.I.IsFloorNodeCleared(roomNode.NodeId);
            bool isConnectedToCurrent = currentNode != null && currentNode.IsConnectedTo(roomNode);
            bool isLockedByMissingKey = roomNode.LoadsSceneDirectly && !RunManager.I.HasFloorKey;
            bool isInteractable = isConnectedToCurrent && !isLockedByMissingKey;
            roomNode.RoomButton.SetState(isInteractable, isCleared, isLockedByMissingKey);
        }

        RefreshKeyIndicator();
    }

    void RefreshKeyIndicator()
    {
        if (floorKeyIndicator != null)
            floorKeyIndicator.gameObject.SetActive(RunManager.I != null && RunManager.I.HasFloorKey);
    }

    RoomNode GetNodeById(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
            return null;

        string normalizedNodeId = nodeId.Trim();

        for (int i = 0; i < roomNodes.Count; i++)
        {
            RoomNode roomNode = roomNodes[i];
            if (roomNode != null && string.Equals(roomNode.NodeId != null ? roomNode.NodeId.Trim() : string.Empty, normalizedNodeId, System.StringComparison.OrdinalIgnoreCase))
                return roomNode;
        }

        return null;
    }
}
