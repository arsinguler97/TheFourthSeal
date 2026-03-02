using UnityEngine;

public class RunManager : MonoBehaviour
{
    public static RunManager I { get; private set; }

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

    public void SelectRoom(RoomTemplateSO template)
    {
        SelectedRoomTemplate = template;
    }
}