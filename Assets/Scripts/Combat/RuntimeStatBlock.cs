using System.Collections.Generic;

public class RuntimeStatBlock
{
    readonly StatBlockData _baseStats;
    readonly List<StatModifierData> _modifiers = new List<StatModifierData>();

    public RuntimeStatBlock(StatBlockData baseStats)
    {
        _baseStats = baseStats;
    }

    public int Health => GetStat(StatType.Health);
    public int Attack => GetStat(StatType.Attack);
    public int Defence => GetStat(StatType.Defence);
    public int Speed => GetStat(StatType.Speed);
    public int Strength => GetStat(StatType.Strength);
    public int Range => GetStat(StatType.Range);
    public int ActionPoints => GetStat(StatType.ActionPoints);

    public void SetModifiers(IEnumerable<StatModifierData> modifiers)
    {
        _modifiers.Clear();

        if (modifiers == null)
            return;

        _modifiers.AddRange(modifiers);
    }

    public int GetStat(StatType statType)
    {
        int value = _baseStats != null ? _baseStats.GetStat(statType) : 0;

        for (int i = 0; i < _modifiers.Count; i++)
        {
            if (_modifiers[i].statType == statType)
                value += _modifiers[i].amount;
        }

        return value;
    }
}
