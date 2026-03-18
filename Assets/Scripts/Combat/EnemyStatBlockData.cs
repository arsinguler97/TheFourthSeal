using UnityEngine;

[System.Serializable]
public class EnemyStatBlockData
{
    public int health = 10;
    public int attack = 1;
    public int defence = 0;
    public int speed = 1;
    public int strength = 0;
    public int range = 1;

    public StatBlockData ToStatBlockData()
    {
        return new StatBlockData
        {
            health = health,
            attack = attack,
            defence = defence,
            speed = speed,
            strength = strength,
            range = range,
            actionPoints = 0
        };
    }
}
