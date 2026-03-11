using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class RoomButton : MonoBehaviour
{
    [FormerlySerializedAs("template")]
    [SerializeField] RoomTemplateSO roomTemplate;

    public void OpenRoom()
    {
        if (roomTemplate == null)
        {
            Debug.LogWarning($"RoomButton on {name} has no RoomTemplateSO assigned.");
            return;
        }

        RunManager.I.SelectRoom(roomTemplate);
        SceneManager.LoadScene("RoomScene");
    }
}
