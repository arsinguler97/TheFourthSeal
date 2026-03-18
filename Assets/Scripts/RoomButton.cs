using UnityEngine;
using UnityEngine.UI;

public class RoomButton : MonoBehaviour
{
    [SerializeField] RoomTemplateSO roomTemplate;
    [SerializeField] Button button;
    [SerializeField] Image nodeImage;
    [SerializeField] Color clearedColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    Color _defaultNodeColor = Color.white;
    bool _isInteractable = true;

    public RoomTemplateSO RoomTemplate => roomTemplate;
    public bool IsInteractable => _isInteractable;

    void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (nodeImage == null)
            nodeImage = GetComponent<Image>();

        if (nodeImage != null)
            _defaultNodeColor = nodeImage.color;
    }

    public void OpenRoom()
    {
        OpenRoomWithEnemyOverride(-1, GetComponent<RoomNode>());
    }

    public void OpenRoomWithEnemyOverride(int enemyCountOverride, RoomNode roomNode = null)
    {
        if (roomTemplate == null)
        {
            Debug.LogWarning($"RoomButton on {name} has no RoomTemplateSO assigned.");
            return;
        }

        if (FloorMapController.I != null)
        {
            FloorMapController.I.SelectRoomNode(roomNode != null ? roomNode : GetComponent<RoomNode>(), roomTemplate, enemyCountOverride);
            return;
        }

        RunManager.I.SelectRoom(roomTemplate, enemyCountOverride);
    }

    public void SetInteractable(bool isInteractable)
    {
        _isInteractable = isInteractable;

        if (button != null)
            button.interactable = isInteractable;

        if (nodeImage != null)
            nodeImage.color = isInteractable ? _defaultNodeColor : clearedColor;
    }
}
