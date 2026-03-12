using UnityEngine;

[System.Serializable]
public class StatBlockData
{
    public int health = 10;
    public int attack = 1;
    public int defence = 0;
    public int speed = 1;
    public int strength = 0;
    public int range = 1;
    public int actionPoints = 2;

    public int GetStat(StatType statType)
    {
        switch (statType)
        {
            case StatType.Health:
                return health;
            case StatType.Attack:
                return attack;
            case StatType.Defence:
                return defence;
            case StatType.Speed:
                return speed;
            case StatType.Strength:
                return strength;
            case StatType.Range:
                return range;
            case StatType.ActionPoints:
                return actionPoints;
            default:
                return 0;
        }
    }
}
