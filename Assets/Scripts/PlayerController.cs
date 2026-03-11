using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour
{
    [FormerlySerializedAs("moveSpeed")]
    [SerializeField] float moveSpeedUnitsPerSecond = 8f;

    Vector2Int _currentGridPosition;
    Vector2Int _destinationGridPosition;
    bool _isMovingToDestination;

    Vector2 _pointerScreenPosition;
    bool _hasPendingClickMove;

    void Start()
    {
        _destinationGridPosition = _currentGridPosition;
        transform.position = GridManager.I.GridToWorld(_currentGridPosition);
    }

    void Update()
    {
        if (_isMovingToDestination)
        {
            Vector3 destinationWorldPosition = GridManager.I.GridToWorld(_destinationGridPosition);
            transform.position = Vector3.MoveTowards(
                transform.position,
                destinationWorldPosition,
                moveSpeedUnitsPerSecond * Time.deltaTime);

            if (Vector3.Distance(transform.position, destinationWorldPosition) < 0.001f)
            {
                _currentGridPosition = _destinationGridPosition;
                _isMovingToDestination = false;
                LoadFloorSceneWhenStandingOnExit();
            }
            return;
        }

        if (_hasPendingClickMove)
        {
            _hasPendingClickMove = false;

            Vector3 clickedWorldPosition = Camera.main.ScreenToWorldPoint(_pointerScreenPosition);
            clickedWorldPosition.z = 0;
            Vector2Int clickedGridPosition = GridManager.I.WorldToGrid(clickedWorldPosition);

            if (GridManager.I.IsWalkable(clickedGridPosition))
            {
                _destinationGridPosition = clickedGridPosition;
                _isMovingToDestination = true;
            }
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
        if (!context.performed || _isMovingToDestination)
            return;

        Vector2Int requestedStep = GetGridStepFromMoveInput(context.ReadValue<Vector2>());
        if (requestedStep == Vector2Int.zero)
            return;

        Vector2Int requestedGridPosition = _currentGridPosition + requestedStep;
        if (!GridManager.I.IsWalkable(requestedGridPosition))
            return;

        _destinationGridPosition = requestedGridPosition;
        _isMovingToDestination = true;
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        _pointerScreenPosition = context.ReadValue<Vector2>();
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        if (context.performed) _hasPendingClickMove = true;
    }

    public void SetGridPosition(Vector2Int gridPos)
    {
        _currentGridPosition = gridPos;
        _destinationGridPosition = gridPos;
        _isMovingToDestination = false;

        if (GridManager.I != null)
            transform.position = GridManager.I.GridToWorld(gridPos);
    }

    void LoadFloorSceneWhenStandingOnExit()
    {
        RoomConfig activeRoomConfig = RunManager.I != null ? RunManager.I.CurrentRoomConfig : null;
        if (activeRoomConfig != null && _currentGridPosition == activeRoomConfig.exitPosition)
            SceneManager.LoadScene("FloorScene");
    }
}
