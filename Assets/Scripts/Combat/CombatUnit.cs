using System.Collections.Generic;
using UnityEngine;

public abstract class CombatUnit : MonoBehaviour
{
    [SerializeField] protected string displayName = "Unit";
    [SerializeField] protected StatBlockData baseStats = new StatBlockData();
    [SerializeField] ParticleSystem hitImpactEffect;
    [SerializeField] SpriteRenderer turnIndicatorRenderer;
    [SerializeField] float turnIndicatorPulseSpeed = 3f;
    [SerializeField] float turnIndicatorMinAlpha = 0.25f;
    [SerializeField] float turnIndicatorMaxAlpha = 1f;

    [Header("Damage Popup")]
    [SerializeField] GameObject damagePopupPrefab;
    [SerializeField] Vector3 damagePopupOffset = new Vector3(0.35f, 0.5f, 0f);
    [SerializeField] Color playerDamagePopupColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] Color enemyDamagePopupColor = new Color(1f, 0.95f, 0.35f, 1f);

    [Header("SFX")]
    [SerializeField] private AudioCue damageSFX;

    readonly List<StatModifierData> _activeModifiers = new List<StatModifierData>();
    RuntimeStatBlock _runtimeStats;
    bool _isTurnIndicatorActive;

    public string DisplayName => displayName;
    public int CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0;
    public RuntimeStatBlock Stats => _runtimeStats;
    public int LastInitiativeRoll { get; private set; }

    public abstract Vector2Int GridPosition { get; }

    public virtual Sprite GetTurnOrderSprite()
    {
        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        return spriteRenderer != null ? spriteRenderer.sprite : null;
    }

    protected virtual void Awake()
    {
        // Runtime stats are copied from the serialized base stat block so modifiers can mutate safely.
        _runtimeStats = new RuntimeStatBlock(baseStats);
        RefreshStats();
        CurrentHealth = Mathf.Max(1, _runtimeStats.Health);
        SetTurnIndicatorActive(false);

        if (hitImpactEffect != null)
            hitImpactEffect.gameObject.SetActive(false);
    }

    protected virtual void Update()
    {
        if (!_isTurnIndicatorActive || turnIndicatorRenderer == null)
            return;

        Color color = turnIndicatorRenderer.color;
        color.a = Mathf.Lerp(
            turnIndicatorMinAlpha,
            turnIndicatorMaxAlpha,
            (Mathf.Sin(Time.time * turnIndicatorPulseSpeed) + 1f) * 0.5f);
        turnIndicatorRenderer.color = color;
    }

    public void RefreshStats()
    {
        _runtimeStats.SetModifiers(_activeModifiers);

        if (CurrentHealth > _runtimeStats.Health)
            CurrentHealth = _runtimeStats.Health;
    }

    public void SetModifiers(IEnumerable<StatModifierData> modifiers)
    {
        _activeModifiers.Clear();

        if (modifiers != null)
            _activeModifiers.AddRange(modifiers);

        RefreshStats();
    }

    public int GetAttackDamage()
    {
        int rolledAttackDamage = Random.Range(1, _runtimeStats.Attack + 1);
        return rolledAttackDamage + _runtimeStats.Strength;
    }

    public int RollInitiative()
    {
        LastInitiativeRoll = Random.Range(1, 21) + _runtimeStats.Speed;
        Debug.Log($"{DisplayName} rolled initiative {LastInitiativeRoll}.");
        return LastInitiativeRoll;
    }

    public void ReceiveDamage(int incomingDamage)
    {
        PlayHitImpactEffect();

        // Defence can reduce damage, but every successful hit still deals at least 1.
        int reducedDamage = incomingDamage - _runtimeStats.Defence;
        int finalDamage = Mathf.Max(1, reducedDamage);
        CurrentHealth = Mathf.Max(0, CurrentHealth - finalDamage);
        SpawnDamagePopup(finalDamage);
        AudioManager.Instance.PlaySound(damageSFX);

        Debug.Log($"{DisplayName} took {finalDamage} damage. Remaining health: {CurrentHealth}.");

        if (!IsAlive)
            HandleDeath();
    }

    void PlayHitImpactEffect()
    {
        if (hitImpactEffect == null)
            return;

        // Spawn a fresh particle instance per hit so replay timing never depends on previous state.
        ParticleSystem impactInstance = Instantiate(
            hitImpactEffect,
            hitImpactEffect.transform.position,
            hitImpactEffect.transform.rotation);

        impactInstance.transform.localScale = hitImpactEffect.transform.lossyScale;
        impactInstance.gameObject.SetActive(true);
        impactInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        impactInstance.Clear(true);
        impactInstance.Play(true);

        float lifetime = impactInstance.main.duration + impactInstance.main.startLifetime.constantMax + 0.25f;
        Destroy(impactInstance.gameObject, lifetime);
    }

    void SpawnDamagePopup(int finalDamage)
    {
        if (damagePopupPrefab == null)
            return;

        // Popup color is chosen by unit side so player and enemy damage read differently at a glance.
        GameObject popupObject = Instantiate(damagePopupPrefab, transform.position + damagePopupOffset, Quaternion.identity);
        DamagePopup popupInstance = popupObject.GetComponent<DamagePopup>();
        if (popupInstance == null)
            return;

        popupInstance.Initialize(finalDamage, this is PlayerUnit ? playerDamagePopupColor : enemyDamagePopupColor);
    }

    protected virtual void HandleDeath()
    {
        SetTurnIndicatorActive(false);
        Debug.Log($"{DisplayName} died.");
        gameObject.SetActive(false);
    }

    public void SetTurnIndicatorActive(bool isActive)
    {
        _isTurnIndicatorActive = isActive;

        if (turnIndicatorRenderer == null)
            return;

        turnIndicatorRenderer.enabled = isActive;

        Color color = turnIndicatorRenderer.color;
        color.a = isActive ? turnIndicatorMaxAlpha : 0f;
        turnIndicatorRenderer.color = color;
    }
}
