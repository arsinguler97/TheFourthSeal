using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Threading.Tasks;

public abstract class CombatUnit : MonoBehaviour
{
    [SerializeField] protected string displayName = "Unit";
    [SerializeField] protected StatBlockData baseStats = new StatBlockData();
    [SerializeField] ParticleSystem hitImpactEffect;
    [SerializeField] SpriteRenderer turnIndicatorRenderer;
    [SerializeField] float turnIndicatorPulseSpeed = 3f;
    [SerializeField] float turnIndicatorMinAlpha = 0.25f;
    [SerializeField] float turnIndicatorMaxAlpha = 1f;
    [SerializeField] Color damageFlashColor = new Color(1f, 0.4f, 0.4f, 1f);
    [SerializeField] float damageFlashDuration = 0.12f;

    private HealthBar _healthBar;
    public DiceCanvas DiceCanvas { get; private set; }

    [Header("Damage Popup")]
    [SerializeField] GameObject damagePopupPrefab;
    [SerializeField] Vector3 damagePopupOffset = new Vector3(0.35f, 0.5f, 0f);
    [SerializeField] Color playerDamagePopupColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] Color enemyDamagePopupColor = new Color(1f, 0.95f, 0.35f, 1f);

    [Header("SFX")]
    [SerializeField] private AudioCue damageSFX;
    [SerializeField] private AudioCue healSFX;

    readonly List<StatModifierData> _activeModifiers = new List<StatModifierData>();
    RuntimeStatBlock _runtimeStats;
    bool _isTurnIndicatorActive;
    Coroutine _damageFlashRoutine;
    SpriteRenderer _damageFlashRenderer;
    Color _defaultDamageFlashRendererColor = Color.white;

    public string DisplayName => displayName;
    public int CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0;
    public RuntimeStatBlock Stats => _runtimeStats;
    public int LastInitiativeRoll { get; set; }
    public int AttackDieSize => GetAttackDieSize();
    public int MaxHealth => _runtimeStats.Health;

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
        _damageFlashRenderer = ResolveDamageFlashRenderer();
        if (_damageFlashRenderer != null)
            _defaultDamageFlashRendererColor = _damageFlashRenderer.color;

        if (hitImpactEffect != null)
            hitImpactEffect.gameObject.SetActive(false);


        _healthBar = GetComponentInChildren<HealthBar>();
        if (_healthBar == null)
            Debug.LogError("CombatUnit missing Canvas_Healthbar Prefab!");
        _healthBar.SetMaxHealth(CurrentHealth);

        DiceCanvas = GetComponentInChildren<DiceCanvas>();
        if (DiceCanvas == null)
            Debug.LogError("CombatUnit missing DiceCanvas!");
        else
            DiceCanvas.gameObject.SetActive(false);
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
        {
            CurrentHealth = _runtimeStats.Health;
        }

        SyncHealthBar();
    }

    public void SetModifiers(IEnumerable<StatModifierData> modifiers)
    {
        _activeModifiers.Clear();

        if (modifiers != null)
            _activeModifiers.AddRange(modifiers);

        RefreshStats();
    }


    protected virtual int GetAttackDieSize()
    {
        return Mathf.Max(1, _runtimeStats.Attack);
    }



    public void ShowDice()
    {
        DiceCanvas.gameObject.SetActive(true);
    }

    public void HideDice()
    {
        DiceCanvas.gameObject.SetActive(false);
    }



    public virtual void ReceiveDamage(int incomingDamage)
    {
        PlayHitImpactEffect();
        PlayDamageFlash();

        // Defence can reduce damage, but every successful hit still deals at least 1.
        int reducedDamage = incomingDamage - _runtimeStats.Defence;
        int finalDamage = Mathf.Max(1, reducedDamage);
        CurrentHealth = Mathf.Max(0, CurrentHealth - finalDamage);
        _healthBar.SetCurrentHealth(CurrentHealth);
        SpawnDamagePopup(finalDamage);
        AudioManager.Instance.PlaySound(damageSFX);

        Debug.Log($"{DisplayName} took {finalDamage} damage. Remaining health: {CurrentHealth}.");

        if (!IsAlive)
            HandleDeath();
    }

    public virtual void ReceiveAttackRoll(CombatUnit attacker, int attackRoll)
    {
        int attackerStrength = attacker != null ? attacker.Stats.Strength : 0;
        int incomingDamage = attackRoll + attackerStrength;
        ReceiveDamage(incomingDamage);
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || !IsAlive)
            return;

        int healedAmount = Mathf.Min(amount, Mathf.Max(0, _runtimeStats.Health - CurrentHealth));
        if (healedAmount <= 0)
            return;

        CurrentHealth += healedAmount;
        SyncHealthBar();
        AudioManager.Instance.PlaySound(healSFX);
        Debug.Log($"{DisplayName} healed {healedAmount}. Current health: {CurrentHealth}.");
    }

    public void SetCurrentHealth(int currentHealth)
    {
        CurrentHealth = Mathf.Clamp(currentHealth, 0, Mathf.Max(1, _runtimeStats.Health));
        SyncHealthBar();
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


        EnemyUnit enemy = this as EnemyUnit;
        if (enemy != null)
            PlayerWallet.I.AddToWallet(enemy.EnemyDefinition.goldValue);
    }

    protected void ResetBaseStats(StatBlockData newBaseStats)
    {
        baseStats = newBaseStats != null ? newBaseStats : new StatBlockData();
        _runtimeStats = new RuntimeStatBlock(baseStats);
        RefreshStats();
        CurrentHealth = Mathf.Max(1, _runtimeStats.Health);
        SyncHealthBar();
    }

    void SyncHealthBar()
    {
        if (_healthBar == null)
            return;

        _healthBar.SetMaxHealth(Mathf.Max(1, _runtimeStats.Health));
        _healthBar.SetCurrentHealth(CurrentHealth);
    }

    void PlayDamageFlash()
    {
        if (_damageFlashRenderer == null)
            return;

        if (_damageFlashRoutine != null)
            StopCoroutine(_damageFlashRoutine);

        _damageFlashRoutine = StartCoroutine(DamageFlashRoutine());
    }

    IEnumerator DamageFlashRoutine()
    {
        _damageFlashRenderer.color = Color.Lerp(_defaultDamageFlashRendererColor, damageFlashColor, 0.65f);
        yield return new WaitForSeconds(damageFlashDuration);

        if (_damageFlashRenderer != null)
            _damageFlashRenderer.color = _defaultDamageFlashRendererColor;

        _damageFlashRoutine = null;
    }

    SpriteRenderer ResolveDamageFlashRenderer()
    {
        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null && spriteRenderers[i] != turnIndicatorRenderer)
                return spriteRenderers[i];
        }

        return null;
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
