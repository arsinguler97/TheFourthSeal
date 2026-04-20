using UnityEngine;

public enum EnemyAttackStyle
{
    Melee,
    Ranged
}

[CreateAssetMenu(menuName = "ScriptableObjects/EnemyDefinition")]
public class EnemyDefinitionSO : ScriptableObject
{
    public string displayName = "Enemy";
    public EnemyStatBlockData baseStats = new EnemyStatBlockData();
    public int goldValue;
    public EnemyAttackStyle attackStyle = EnemyAttackStyle.Melee;
    public GameObject projectilePrefab;
    public Sprite worldSprite;
    public Sprite turnOrderIcon;
    [TextArea] public string description;
    public bool immuneToOddDamage;
    public int immuneToDamageAtOrBelow;
    public string[] immuneToStatusEffects;
    public GameObject missedHitVfxPrefab;
    public Vector3 missedHitVfxOffset = new Vector3(0f, 0.2f, 0f);

    public bool IsImmuneToStatusEffect(string statusEffectName)
    {
        if (string.IsNullOrWhiteSpace(statusEffectName) || immuneToStatusEffects == null)
            return false;

        for (int i = 0; i < immuneToStatusEffects.Length; i++)
        {
            if (string.Equals(immuneToStatusEffects[i], statusEffectName, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
