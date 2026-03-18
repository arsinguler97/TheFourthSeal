using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/EnemyDefinition")]
public class EnemyDefinitionSO : ScriptableObject
{
    public string displayName = "Enemy";
    public EnemyStatBlockData baseStats = new EnemyStatBlockData();
    public Sprite turnOrderIcon;
    public Sprite cardArt;
    [TextArea] public string description;
}
