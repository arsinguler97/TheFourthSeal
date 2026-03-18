using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FloorMapController : MonoBehaviour
{
    public static FloorMapController I { get; private set; }

    [SerializeField] FloorMapPlayerUI floorMapPlayerUI;
    [SerializeField] RoomNode startingRoomNode;
    [SerializeField] List<RoomNode> roomNodes = new List<RoomNode>();
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

    void Start()
    {
        if (RunManager.I == null)
            return;

        List<string> availableNodeIds = new List<string>();
        for (int i = 0; i < roomNodes.Count; i++)
        {
            RoomNode listedNode = roomNodes[i];
            if (listedNode == null)
                continue;

            availableNodeIds.Add($"{listedNode.name}:{listedNode.NodeId}");
        }

        Debug.Log($"FloorMapController available room nodes -> [{string.Join(", ", availableNodeIds)}], startingRoomNode '{(startingRoomNode != null ? startingRoomNode.name : "null")}:{(startingRoomNode != null ? startingRoomNode.NodeId : "null")}'.");

        RefreshNodeStates();

        RoomNode currentNode = GetNodeById(RunManager.I.CurrentFloorNodeId);
        if (currentNode == null)
            currentNode = startingRoomNode;

        Debug.Log($"FloorMapController start node resolved to: {(currentNode != null ? currentNode.NodeId : "null")} from CurrentFloorNodeId '{RunManager.I.CurrentFloorNodeId}'.");

        if (floorMapPlayerUI != null && currentNode != null)
        {
            RectTransform targetRect = currentNode.GetComponent<RectTransform>();
            Debug.Log($"FloorMapController snapping marker to node '{currentNode.name}' / '{currentNode.NodeId}' at anchoredPosition '{(targetRect != null ? targetRect.anchoredPosition.ToString() : "null")}'.");
            floorMapPlayerUI.SnapTo(currentNode.GetComponent<RectTransform>());
        }
    }

    public void SelectRoomNode(RoomNode roomNode, RoomTemplateSO roomTemplate, int enemyCountOverride)
    {
        if (_isTransitioningToRoom || roomNode == null || roomTemplate == null || RunManager.I == null)
            return;

        if (string.IsNullOrWhiteSpace(roomNode.NodeId))
        {
            Debug.LogWarning($"SelectRoomNode aborted because '{roomNode.name}' has an empty NodeId.");
            return;
        }

        Debug.Log($"SelectRoomNode -> object '{roomNode.name}', nodeId '{roomNode.NodeId}', currentFloorNodeId '{RunManager.I.CurrentFloorNodeId}'.");

        if (RunManager.I.IsFloorNodeCleared(roomNode.NodeId))
            return;

        RunManager.I.PrepareRoomSelection(roomNode.NodeId, roomTemplate, enemyCountOverride);
        _isTransitioningToRoom = true;

        RectTransform targetRect = roomNode.GetComponent<RectTransform>();
        if (floorMapPlayerUI != null && targetRect != null)
        {
            floorMapPlayerUI.MoveTo(targetRect, () => SceneManager.LoadScene("RoomScene"));
            return;
        }

        SceneManager.LoadScene("RoomScene");
    }

    void RefreshNodeStates()
    {
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
            roomNode.RoomButton.SetInteractable(!isCleared && isConnectedToCurrent);
        }
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
