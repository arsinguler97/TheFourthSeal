using System.Collections.Generic;
using UnityEngine;

public abstract class CombatUnit : MonoBehaviour
{
    [SerializeField] string displayName = "Unit";
    [SerializeField] StatBlockData baseStats = new StatBlockData();

    readonly List<StatModifierData> _activeModifiers = new List<StatModifierData>();
    RuntimeStatBlock _runtimeStats;

    public string DisplayName => displayName;
    public int CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0;
    public RuntimeStatBlock Stats => _runtimeStats;
    public int LastInitiativeRoll { get; private set; }

    public abstract Vector2Int GridPosition { get; }

    protected virtual void Awake()
    {
        _runtimeStats = new RuntimeStatBlock(baseStats);
        RefreshStats();
        CurrentHealth = Mathf.Max(1, _runtimeStats.Health);
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
        int reducedDamage = incomingDamage - _runtimeStats.Defence;
        int finalDamage = Mathf.Max(1, reducedDamage);
        CurrentHealth = Mathf.Max(0, CurrentHealth - finalDamage);

        Debug.Log($"{DisplayName} took {finalDamage} damage. Remaining health: {CurrentHealth}.");

        if (!IsAlive)
            HandleDeath();
    }

    protected virtual void HandleDeath()
    {
        Debug.Log($"{DisplayName} died.");
        gameObject.SetActive(false);
    }
}
