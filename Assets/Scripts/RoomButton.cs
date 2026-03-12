using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomButton : MonoBehaviour
{
    [SerializeField] RoomTemplateSO roomTemplate;
    public RoomTemplateSO RoomTemplate => roomTemplate;

    public void OpenRoom()
    {
        OpenRoomWithEnemyOverride(-1);
    }

    public void OpenRoomWithEnemyOverride(int enemyCountOverride)
    {
        if (roomTemplate == null)
        {
            Debug.LogWarning($"RoomButton on {name} has no RoomTemplateSO assigned.");
            return;
        }

        // Save the chosen template before switching scenes so RoomGenerator can read it later.
        RunManager.I.SelectRoom(roomTemplate, enemyCountOverride);
        SceneManager.LoadScene("RoomScene");
    }
}
