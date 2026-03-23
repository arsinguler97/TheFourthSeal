using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/EnemyDefinition")]
public class EnemyDefinitionSO : ScriptableObject
{
    public string displayName = "Enemy";
    public EnemyStatBlockData baseStats = new EnemyStatBlockData();
    public Sprite worldSprite;
    public Sprite turnOrderIcon;
    [TextArea] public string description;
}
