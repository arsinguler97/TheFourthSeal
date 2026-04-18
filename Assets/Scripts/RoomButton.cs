using UnityEngine;
using UnityEngine.UI;

public class RoomButton : MonoBehaviour
{
    [SerializeField] RoomTemplateSO roomTemplate;
    [SerializeField] Button button;
    [SerializeField] Image nodeImage;
    [SerializeField] Color clearedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField] Color lockedColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    [SerializeField] AudioCue clickSFX;

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

    private void Start()
    {
        if (button)
            button.onClick.AddListener(() => AudioManager.Instance.PlaySound(clickSFX));
    }


    public void OpenRoom()
    {
        RoomNode roomNode = GetComponent<RoomNode>();
        int enemyCountOverride = roomNode != null ? roomNode.GetEnemyCountOverride() : -1;
        OpenRoomWithEnemyOverride(enemyCountOverride, roomNode);
    }

    public void OpenRoomWithEnemyOverride(int enemyCountOverride, RoomNode roomNode = null)
    {
        RoomNode resolvedRoomNode = roomNode != null ? roomNode : GetComponent<RoomNode>();
        if (resolvedRoomNode != null && resolvedRoomNode.LoadsSceneDirectly)
        {
            if (FloorMapController.I != null)
            {
                FloorMapController.I.SelectRoomNode(resolvedRoomNode, null, -1);
                return;
            }

            Debug.LogWarning($"RoomButton on {name} could not route to scene '{resolvedRoomNode.DestinationSceneName}' because no active FloorMapController was found.");
            return;
        }

        if (roomTemplate == null)
        {
            Debug.LogWarning($"RoomButton on {name} has no RoomTemplateSO assigned.");
            return;
        }

        if (FloorMapController.I != null)
        {
            FloorMapController.I.SelectRoomNode(resolvedRoomNode, roomTemplate, enemyCountOverride);
            return;
        }

        Debug.LogWarning($"RoomButton on {name} could not open a room because no active FloorMapController was found.");
    }

    public void SetState(bool isInteractable, bool isCleared, bool isLocked = false)
    {
        _isInteractable = isInteractable;

        if (button != null)
            button.interactable = isInteractable;

        if (nodeImage != null)
            nodeImage.color = isCleared ? clearedColor : (isLocked ? lockedColor : _defaultNodeColor);
    }
}
