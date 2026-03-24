using UnityEngine;
using System.Collections.Generic;

public class RoomNode : MonoBehaviour
{
    [SerializeField] string nodeId;
    [SerializeField] RoomButton roomButton;
    [SerializeField] int additionalEnemyCount;
    [SerializeField] List<RoomNode> connectedNodes = new List<RoomNode>();
    [SerializeField] List<EnemyDefinitionSO> enemyOverrides = new List<EnemyDefinitionSO>();

    public string NodeId => nodeId;
    public RoomButton RoomButton => roomButton;
    public IReadOnlyList<RoomNode> ConnectedNodes => connectedNodes;
    public IReadOnlyList<EnemyDefinitionSO> EnemyOverrides => enemyOverrides;

    // Final enemy count for this specific node is template base plus this node bonus.
    public int AdditionalEnemyCount => additionalEnemyCount;

    void Reset()
    {
        // Auto-fill the reference when the component is first added in the editor.
        roomButton = GetComponent<RoomButton>();
    }

    void OnMouseDown()
    {
        // This lets a world-space room node forward clicks to the same open-room logic as UI buttons.
        if (roomButton != null && roomButton.IsInteractable)
            roomButton.OpenRoomWithEnemyOverride(GetEnemyCountOverride(), this);
    }

    public int GetEnemyCountOverride()
    {
        if (roomButton == null || roomButton.RoomTemplate == null)
            return -1;

        return Mathf.Max(0, roomButton.RoomTemplate.enemyCount + additionalEnemyCount);
    }

    public bool IsConnectedTo(RoomNode otherNode)
    {
        return otherNode != null && connectedNodes.Contains(otherNode);
    }
}
