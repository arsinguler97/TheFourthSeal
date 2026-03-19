using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerUnit : CombatUnit
{
    [Header("Death VFX")]
    [SerializeField] GameObject deathVfxPrefab;
    [SerializeField] Vector3 deathVfxOffset = new Vector3(0f, 0.2f, 0f);

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

    protected override void HandleDeath()
    {
        if (deathVfxPrefab != null)
            Instantiate(deathVfxPrefab, transform.position + deathVfxOffset, Quaternion.identity);

        if (CombatManager.I != null)
            CombatManager.I.HandlePlayerDeath(this);

        base.HandleDeath();
    }
}
