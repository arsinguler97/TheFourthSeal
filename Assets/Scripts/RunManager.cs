using UnityEngine;

public class RunManager : MonoBehaviour
{
    public static RunManager I { get; private set; }

    // Persists the room choice made on the floor scene so RoomScene can build matching content.
    public RoomTemplateSO SelectedRoomTemplate { get; private set; }
    public RoomConfig CurrentRoomConfig;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SelectRoom(RoomTemplateSO roomTemplate)
    {
        SelectedRoomTemplate = roomTemplate;
    }
}
