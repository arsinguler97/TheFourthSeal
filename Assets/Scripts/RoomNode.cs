using UnityEngine;
using UnityEngine.Serialization;

public class RoomNode : MonoBehaviour
{
    [FormerlySerializedAs("roomIndex")]
    [SerializeField] int roomIndex;
    [FormerlySerializedAs("roomButton")]
    [SerializeField] RoomButton roomButton;

    void Reset()
    {
        roomButton = GetComponent<RoomButton>();
    }

    void OnMouseDown()
    {
        // This lets a world-space room node forward clicks to the same open-room logic as UI buttons.
        if (roomButton != null)
            roomButton.OpenRoom();
    }
}
