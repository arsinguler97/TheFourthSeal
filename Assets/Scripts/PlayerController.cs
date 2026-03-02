using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 8f;

    Vector2Int _gridPos;
    Vector2Int _targetGridPos;
    bool _moving;

    Vector2 _look;
    bool _click;

    void Start()
    {
        _targetGridPos = _gridPos;
        transform.position = GridManager.I.GridToWorld(_gridPos);
    }

    void Update()
    {
        if (_moving)
        {
            var targetWorld = GridManager.I.GridToWorld(_targetGridPos);
            transform.position = Vector3.MoveTowards(transform.position, targetWorld, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetWorld) < 0.001f)
            {
                _gridPos = _targetGridPos;
                _moving = false;
                CheckExitReached();
            }
            return;
        }

        if (_click)
        {
            _click = false;

            var w = Camera.main.ScreenToWorldPoint(_look);
            w.z = 0;
            var g = GridManager.I.WorldToGrid(w);

            if (GridManager.I.IsWalkable(g))
            {
                _targetGridPos = g;
                _moving = true;
            }
            return;
        }

    }

    Vector2Int ReadStepFromMove(Vector2 v)
    {
        if (v == Vector2.zero) return Vector2Int.zero;

        if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
            return v.x > 0 ? Vector2Int.right : Vector2Int.left;

        return v.y > 0 ? Vector2Int.up : Vector2Int.down;
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || _moving)
            return;

        var step = ReadStepFromMove(ctx.ReadValue<Vector2>());
        if (step == Vector2Int.zero)
            return;

        var next = _gridPos + step;
        if (!GridManager.I.IsWalkable(next))
            return;

        _targetGridPos = next;
        _moving = true;
    }

    public void OnLook(InputAction.CallbackContext ctx)
    {
        _look = ctx.ReadValue<Vector2>();
    }

    public void OnClick(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) _click = true;
    }

    public void SetGridPosition(Vector2Int gridPos)
    {
        _gridPos = gridPos;
        _targetGridPos = gridPos;
        _moving = false;

        if (GridManager.I != null)
            transform.position = GridManager.I.GridToWorld(gridPos);
    }

    void CheckExitReached()
    {
        var cfg = RunManager.I != null ? RunManager.I.CurrentRoomConfig : null;
        if (cfg != null && _gridPos == cfg.exit)
            SceneManager.LoadScene("FloorScene");
    }
}
