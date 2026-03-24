using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/EnemyDefinition")]
public class EnemyDefinitionSO : ScriptableObject
{
    public string displayName = "Enemy";
    public EnemyStatBlockData baseStats = new EnemyStatBlockData();
    public Sprite worldSprite;
    public Sprite turnOrderIcon;
    [TextArea] public string description;
    public bool immuneToOddDamage;
    public int immuneToDamageAtOrBelow;
    public GameObject missedHitVfxPrefab;
    public Vector3 missedHitVfxOffset = new Vector3(0f, 0.2f, 0f);
}
