using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    // How fast the player travels between two grid cells.
    [SerializeField] float moveSpeedUnitsPerSecond = 8f;

    // The cell the player is currently standing on.
    Vector2Int _currentGridPosition;

    // The next cell the player is moving toward.
    Vector2Int _destinationGridPosition;

    // Prevents new movement input while the player is already traveling.
    bool _isMovingToDestination;

    // Latest pointer position from the input system.
    Vector2 _pointerScreenPosition;

    // Marks that a click happened and should be handled in Update.
    bool _hasPendingClickMove;

    public Vector2Int CurrentGridPosition => _currentGridPosition;
    public bool IsMovingToDestination => _isMovingToDestination;


    // SFX
    [SerializeField] private AudioCue footstepSFX;
    PlayerUnit _playerUnit;



    void Start()
    {
        _playerUnit = GetComponent<PlayerUnit>();

        // Keep the player snapped to the same grid cell and world position when the room first loads.
        _destinationGridPosition = _currentGridPosition;
        transform.position = GridManager.I.GridToWorld(_currentGridPosition);
    }

    void Update()
    {
        // While moving, keep stepping toward the chosen destination cell.
        if (_isMovingToDestination)
        {
            // Movement is always resolved in world space, but the destination comes from a grid cell.
            Vector3 destinationWorldPosition = GridManager.I.GridToWorld(_destinationGridPosition);
            transform.position = Vector3.MoveTowards(
                transform.position,
                destinationWorldPosition,
                moveSpeedUnitsPerSecond * Time.deltaTime);

            TryCollectKeyTileAtCurrentPosition();
            TryOpenRewardTileAtCurrentPosition();

            if (Vector3.Distance(transform.position, destinationWorldPosition) < 0.001f)
            {
                _currentGridPosition = _destinationGridPosition;
                _isMovingToDestination = false;

                if (CombatManager.I != null && _playerUnit != null)
                    CombatManager.I.ApplyLavaDamageIfNeeded(_playerUnit);

                LoadFloorSceneWhenStandingOnExit();
            }

            return;
        }

        // Clicks are interpreted according to the currently selected action mode.
        if (_hasPendingClickMove)
        {
            // Click input is stored by the input callback and consumed here once per frame.
            _hasPendingClickMove = false;

            Vector2 pointerScreenPosition = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : _pointerScreenPosition;

            Vector3 clickedWorldPosition = Camera.main.ScreenToWorldPoint(pointerScreenPosition);
            clickedWorldPosition.z = 0;
            Vector2Int clickedGridPosition = GridManager.I.WorldToGrid(clickedWorldPosition);

            if (TurnManager.I != null && TurnManager.I.IsPlayerMoveModeActive)
                TryHandleMoveClick(clickedGridPosition);
            else if (TurnManager.I != null && TurnManager.I.IsPlayerAttackModeActive)
                TryHandleAttackClick(clickedGridPosition);

            return;
        }
    }

    // Converts an analog input vector into a single grid step on the dominant axis.
    Vector2Int GetGridStepFromMoveInput(Vector2 moveInput)
    {
        if (moveInput == Vector2.zero) return Vector2Int.zero;

        if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
            return moveInput.x > 0 ? Vector2Int.right : Vector2Int.left;

        return moveInput.y > 0 ? Vector2Int.up : Vector2Int.down;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        // Keyboard or stick input requests exactly one neighbour cell per performed action.
        if (!context.performed || _isMovingToDestination)
            return;

        if (EquipmentManager.Instance != null && !EquipmentManager.Instance.IsLoadoutLockedForCurrentRoom)
            return;

        if (TurnManager.I == null || !TurnManager.I.CanPlayerSpendMoveStep(1))
        {
            Debug.Log("Move input ignored because move mode is not active or no move steps remain.");
            return;
        }

        Vector2Int requestedStep = GetGridStepFromMoveInput(context.ReadValue<Vector2>());
        if (requestedStep == Vector2Int.zero)
            return;

        if (TryMoveOneStep(requestedStep))
        {
            TurnManager.I.NotifyPlayerStartedMoveAction();
            TurnManager.I.NotifyPlayerMovedStep(1);
        }
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        // Pointer position is cached so click-to-move can convert it into a grid cell in Update.
        _pointerScreenPosition = context.ReadValue<Vector2>();
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        // Do not move immediately here; just queue the click so gameplay stays in one place: Update.
        if (EquipmentManager.Instance != null && !EquipmentManager.Instance.IsLoadoutLockedForCurrentRoom)
            return;

        if (context.performed) _hasPendingClickMove = true;
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.performed || TurnManager.I == null)
            return;

        if (EquipmentManager.Instance != null && !EquipmentManager.Instance.IsLoadoutLockedForCurrentRoom)
            return;

        TurnManager.I.SelectAttackAction();
    }

    public void SelectMoveAction()
    {
        if (TurnManager.I != null)
            TurnManager.I.SelectMoveAction();
    }

    public void SelectAttackAction()
    {
        if (TurnManager.I != null)
            TurnManager.I.SelectAttackAction();
    }

    public void UseConsumableAction()
    {
        if (TurnManager.I != null)
            TurnManager.I.ExecuteConsumableAction();
    }

    public void SetGridPosition(Vector2Int gridPos)
    {
        // Used by RoomGenerator to place the player instantly on the generated spawn tile.
        _currentGridPosition = gridPos;
        _destinationGridPosition = gridPos;
        _isMovingToDestination = false;

        if (GridManager.I != null)
            transform.position = GridManager.I.GridToWorld(gridPos);
    }

    void LoadFloorSceneWhenStandingOnExit()
    {
        // Reaching the exit finishes the current room and returns to the floor selection scene.
        RoomConfig activeRoomConfig = RunManager.I != null ? RunManager.I.CurrentRoomConfig : null;
        if (activeRoomConfig != null && _currentGridPosition == activeRoomConfig.exit)
        {
            if (CombatManager.I != null)
                CombatManager.I.BeginReturnToFloorSceneTransition();
            else
                SceneManager.LoadScene("FloorScene");
        }
    }

    void TryOpenRewardTileAtCurrentPosition()
    {
        RoomConfig activeRoomConfig = RunManager.I != null ? RunManager.I.CurrentRoomConfig : null;
        if (activeRoomConfig == null || activeRoomConfig.isRewardOpened || GridManager.I.WorldToGrid(transform.position) != activeRoomConfig.reward)
            return;

        activeRoomConfig.isRewardOpened = true;

        if (GridManager.I != null)
            GridManager.I.RefreshRewardTile(activeRoomConfig);

        if (_playerUnit != null)
            _playerUnit.PlayRewardOpenVfx();
    }

    void TryCollectKeyTileAtCurrentPosition()
    {
        RoomConfig activeRoomConfig = RunManager.I != null ? RunManager.I.CurrentRoomConfig : null;
        if (activeRoomConfig == null
            || !activeRoomConfig.hasKeyTile
            || activeRoomConfig.isKeyCollected
            || GridManager.I.WorldToGrid(transform.position) != activeRoomConfig.keyTile)
            return;

        activeRoomConfig.isKeyCollected = true;

        if (RunManager.I != null)
            RunManager.I.AcquireFloorKey();

        if (GridManager.I != null)
            GridManager.I.RefreshKeyTile(activeRoomConfig);

        if (_playerUnit != null)
            _playerUnit.PlayRewardOpenVfx();
    }

    bool TryMoveOneStep(Vector2Int requestedStep)
    {
        if (!GridMovementUtility.IsSingleCardinalStep(requestedStep))
        {
            Debug.Log($"Move blocked: {requestedStep} is not a single adjacent step.");
            return false;
        }

        Vector2Int requestedGridPosition = _currentGridPosition + requestedStep;
        return TryStartMoveToGridPosition(requestedGridPosition);
    }

    bool TryStartMoveToGridPosition(Vector2Int requestedGridPosition)
    {
        if (!GridMovementUtility.CanUnitEnterTile(requestedGridPosition, GetComponent<PlayerUnit>()))
        {
            Debug.Log($"Move blocked: {requestedGridPosition} cannot be entered.");
            return false;
        }

        _destinationGridPosition = requestedGridPosition;
        _isMovingToDestination = true;

        if (AudioManager.Instance != null && footstepSFX != null)
            AudioManager.Instance.PlaySound(footstepSFX);

        Debug.Log($"Player moving to {requestedGridPosition}.");
        return true;
    }

    void TryHandleMoveClick(Vector2Int clickedGridPosition)
    {
        int manhattanDistance = Mathf.Abs(clickedGridPosition.x - _currentGridPosition.x)
            + Mathf.Abs(clickedGridPosition.y - _currentGridPosition.y);
        if (manhattanDistance != 1)
        {
            Debug.Log($"Move click ignored because {clickedGridPosition} is not adjacent.");
            return;
        }

        if (!TurnManager.I.CanPlayerSpendMoveStep(1))
            return;

        Vector2Int requestedStep = clickedGridPosition - _currentGridPosition;
        if (TryMoveOneStep(requestedStep))
        {
            TurnManager.I.NotifyPlayerStartedMoveAction();
            TurnManager.I.NotifyPlayerMovedStep(1);
        }
    }

    void TryHandleAttackClick(Vector2Int clickedGridPosition)
    {
        if (CombatManager.I == null)
            return;

        // TurnManager only resolves the action cost after CombatManager confirms a valid hit.
        if (CombatManager.I.TryPlayerAttackGrid(clickedGridPosition))
            TurnManager.I.NotifyPlayerAttackResolved();
        else
            Debug.Log($"Attack click on {clickedGridPosition} did not hit a valid enemy target.");
    }
}
