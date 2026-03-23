using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerUnit : CombatUnit
{
    [Header("Death VFX")]
    [SerializeField] GameObject deathVfxPrefab;
    [SerializeField] Vector3 deathVfxOffset = new Vector3(0f, 0.2f, 0f);

    [Header("Action VFX")]
    [SerializeField] GameObject consumableUseVfxPrefab;
    [SerializeField] Vector3 consumableUseVfxOffset = new Vector3(0f, 0.2f, 0f);
    [SerializeField] GameObject rewardOpenVfxPrefab;
    [SerializeField] Vector3 rewardOpenVfxOffset = new Vector3(0f, 0.2f, 0f);

    PlayerController _playerController;

    public override Vector2Int GridPosition => _playerController != null ? _playerController.CurrentGridPosition : Vector2Int.zero;
    public bool IsMoving => _playerController != null && _playerController.IsMovingToDestination;

    protected override void Awake()
    {
        base.Awake();
        _playerController = GetComponent<PlayerController>();

        if (CombatManager.I != null)
            CombatManager.I.RegisterPlayer(this);

        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.ApplyEquippedStatsToPlayer(this);
    }

    private void Start()
    {
        if (RunManager.I != null)
            SetCurrentHealth(RunManager.I.GetPlayerHealthForNextRoom(MaxHealth));
    }

    protected override int GetAttackDieSize()
    {
        if (EquipmentManager.Instance != null)
        {
            int weaponAttackOverride = EquipmentManager.Instance.GetEquippedWeaponAttackOverride();
            if (weaponAttackOverride > 0)
                return weaponAttackOverride;
        }

        return base.GetAttackDieSize();
    }

    public void PlayConsumableUseVfx()
    {
        PlayOneShotVfx(consumableUseVfxPrefab, consumableUseVfxOffset);
    }

    public void PlayRewardOpenVfx()
    {
        PlayOneShotVfx(rewardOpenVfxPrefab, rewardOpenVfxOffset);
    }

    protected override void HandleDeath()
    {
        if (deathVfxPrefab != null)
            Instantiate(deathVfxPrefab, transform.position + deathVfxOffset, Quaternion.identity);

        if (CombatManager.I != null)
            CombatManager.I.HandlePlayerDeath(this);

        base.HandleDeath();
    }

    void PlayOneShotVfx(GameObject vfxPrefab, Vector3 offset)
    {
        if (vfxPrefab != null)
            Instantiate(vfxPrefab, transform.position + offset, Quaternion.identity);
    }
}
