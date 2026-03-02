using UnityEngine;

public class RoomNode : MonoBehaviour
{
    [SerializeField] int roomIndex;
    [SerializeField] RoomButton roomButton;

    void Reset()
    {
        roomButton = GetComponent<RoomButton>();
    }

    void OnMouseDown()
    {
        if (roomButton != null)
            roomButton.OpenRoom();
    }
}
