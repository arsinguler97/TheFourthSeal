using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ItemStatBlockData
{
    public int attack = 0;
    public int defence = 0;
    public int speed = 0;
    public int strength = 0;
    public int range = 0;
    public int actionPoints = 0;

    public StatBlockData ToStatBlockData()
    {
        return new StatBlockData
        {
            attack = attack,
            defence = defence,
            speed = speed,
            strength = strength,
            range = range,
            actionPoints = actionPoints
        };
    }

    public List<StatModifierData> ToModifiers()
    {
        return ToModifiers(true);
    }

    public List<StatModifierData> ToModifiers(bool includeAttack)
    {
        List<StatModifierData> modifiers = new List<StatModifierData>();
        if (includeAttack)
            AddModifierIfNeeded(modifiers, StatType.Attack, attack);
        AddModifierIfNeeded(modifiers, StatType.Defence, defence);
        AddModifierIfNeeded(modifiers, StatType.Speed, speed);
        AddModifierIfNeeded(modifiers, StatType.Strength, strength);
        AddModifierIfNeeded(modifiers, StatType.Range, range);
        AddModifierIfNeeded(modifiers, StatType.ActionPoints, actionPoints);
        return modifiers;
    }

    static void AddModifierIfNeeded(List<StatModifierData> modifiers, StatType statType, int amount)
    {
        if (amount == 0)
            return;

        modifiers.Add(new StatModifierData
        {
            statType = statType,
            amount = amount
        });
    }
}
