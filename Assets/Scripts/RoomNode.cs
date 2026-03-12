using UnityEngine;

public class RoomNode : MonoBehaviour
{
    [SerializeField] RoomButton roomButton;
    [SerializeField] int additionalEnemyCount;

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
        if (roomButton != null)
            roomButton.OpenRoomWithEnemyOverride(GetEnemyCountOverride());
    }

    int GetEnemyCountOverride()
    {
        if (roomButton == null || roomButton.RoomTemplate == null)
            return -1;

        return Mathf.Max(0, roomButton.RoomTemplate.enemyCount + additionalEnemyCount);
    }
}
