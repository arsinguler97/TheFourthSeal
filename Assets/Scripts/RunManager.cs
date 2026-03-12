using UnityEngine;

public class RunManager : MonoBehaviour
{
    public static RunManager I { get; private set; }

    // Stores the room selected on the floor map so the next scene knows what to generate.
    public RoomTemplateSO SelectedRoomTemplate { get; private set; }
    public int SelectedRoomEnemyCount { get; private set; }

    // Holds the concrete positions generated for the currently active room.
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

    public void SelectRoom(RoomTemplateSO roomTemplate, int enemyCountOverride = -1)
    {
        SelectedRoomTemplate = roomTemplate;
        SelectedRoomEnemyCount = enemyCountOverride >= 0 ? enemyCountOverride : roomTemplate.enemyCount;
    }
}
