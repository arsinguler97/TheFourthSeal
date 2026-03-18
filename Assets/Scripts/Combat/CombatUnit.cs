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
        _runtimeStats = new RuntimeStatBlock(baseStats);
        RefreshStats();
        CurrentHealth = Mathf.Max(1, _runtimeStats.Health);
        SetTurnIndicatorActive(false);
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

        int reducedDamage = incomingDamage - _runtimeStats.Defence;
        int finalDamage = Mathf.Max(1, reducedDamage);
        CurrentHealth = Mathf.Max(0, CurrentHealth - finalDamage);

        Debug.Log($"{DisplayName} took {finalDamage} damage. Remaining health: {CurrentHealth}.");

        if (!IsAlive)
            HandleDeath();
    }

    void PlayHitImpactEffect()
    {
        if (hitImpactEffect == null)
            return;

        hitImpactEffect.Play();
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
