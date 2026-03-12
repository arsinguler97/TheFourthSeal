using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerUnit : CombatUnit
{
    PlayerController _playerController;

    public override Vector2Int GridPosition => _playerController != null ? _playerController.CurrentGridPosition : Vector2Int.zero;
    public bool IsMoving => _playerController != null && _playerController.IsMovingToDestination;

    protected override void Awake()
    {
        base.Awake();
        _playerController = GetComponent<PlayerController>();

        if (CombatManager.I != null)
            CombatManager.I.RegisterPlayer(this);
    }
}
