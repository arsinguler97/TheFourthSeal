using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerUnit : CombatUnit
{
    [SerializeField] private AudioCue rewardPickupSFX;

    [Header("Death VFX")]
    [SerializeField] GameObject deathVfxPrefab;
    [SerializeField] Vector3 deathVfxOffset = new Vector3(0f, 0.2f, 0f);

    [Header("Action VFX")]
    [SerializeField] GameObject consumableUseVfxPrefab;
    [SerializeField] Vector3 consumableUseVfxOffset = new Vector3(0f, 0.2f, 0f);
    [SerializeField] GameObject rewardOpenVfxPrefab;
    [SerializeField] Vector3 rewardOpenVfxOffset = new Vector3(0f, 0.2f, 0f);

    PlayerController _playerController;
    SpriteRenderer _visualSpriteRenderer;

    public override Vector2Int GridPosition => _playerController != null ? _playerController.CurrentGridPosition : Vector2Int.zero;
    public bool IsMoving => _playerController != null && _playerController.IsMovingToDestination;

    protected override void Awake()
    {
        base.Awake();
        _playerController = GetComponent<PlayerController>();
        _visualSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

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
        AudioManager.Instance.PlaySound(rewardPickupSFX);
        PlayOneShotVfx(consumableUseVfxPrefab, consumableUseVfxOffset);
    }

    public void PlayRewardOpenVfx()
    {
        AudioManager.Instance.PlaySound(rewardPickupSFX);
        PlayOneShotVfx(rewardOpenVfxPrefab, rewardOpenVfxOffset);
    }

    protected override void HandleDeath()
    {
        if (deathVfxPrefab != null)
            PlayOneShotVfx(deathVfxPrefab, deathVfxOffset);

        if (CombatManager.I != null)
            CombatManager.I.HandlePlayerDeath(this);

        base.HandleDeath();
    }

    void PlayOneShotVfx(GameObject vfxPrefab, Vector3 offset)
    {
        if (vfxPrefab == null)
            return;

        GameObject vfxInstance = Instantiate(vfxPrefab, transform.position + offset, Quaternion.identity, transform);
        vfxInstance.transform.localPosition = offset;
        MatchVfxSorting(vfxInstance);
    }

    void MatchVfxSorting(GameObject vfxInstance)
    {
        if (vfxInstance == null || _visualSpriteRenderer == null)
            return;

        ParticleSystemRenderer[] particleRenderers = vfxInstance.GetComponentsInChildren<ParticleSystemRenderer>(true);
        for (int i = 0; i < particleRenderers.Length; i++)
        {
            particleRenderers[i].sortingLayerID = _visualSpriteRenderer.sortingLayerID;
            particleRenderers[i].sortingOrder = _visualSpriteRenderer.sortingOrder + 1;
        }

        SpriteRenderer[] spriteRenderers = vfxInstance.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            spriteRenderers[i].sortingLayerID = _visualSpriteRenderer.sortingLayerID;
            spriteRenderers[i].sortingOrder = _visualSpriteRenderer.sortingOrder + 1;
        }
    }
}
