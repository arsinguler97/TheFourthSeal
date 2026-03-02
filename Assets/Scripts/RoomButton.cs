using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomButton : MonoBehaviour
{
    [SerializeField] RoomTemplateSO template;

    public void OpenRoom()
    {
        if (template == null)
        {
            Debug.LogWarning($"RoomButton on {name} has no RoomTemplateSO assigned.");
            return;
        }

        RunManager.I.SelectRoom(template);
        SceneManager.LoadScene("RoomScene");
    }
}
