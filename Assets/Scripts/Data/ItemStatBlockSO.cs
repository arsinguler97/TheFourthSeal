using UnityEngine;

[System.Serializable]
public class ItemStatBlockData
{
    public int defence = 0;
    public int speed = 0;
    public int strength = 0;
    public int range = 0;

    public StatBlockData ToStatBlockData()
    {
        return new StatBlockData
        {
            health = 0,
            attack = 0,
            defence = defence,
            speed = speed,
            strength = strength,
            range = range,
            actionPoints = 0
        };
    }
}
