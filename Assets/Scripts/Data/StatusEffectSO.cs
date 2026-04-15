using UnityEngine;

[CreateAssetMenu(fileName = "NewStatusEffect", menuName = "ScriptableObjects/StatusEffectDefinition")]
public class StatusEffectSO : ScriptableObject
{
    public string Name;
    public int TurnAffectedCount;
    public ParticleSystem ParticleEffect;

    public int DotAmount;
    public bool IsStun;
}
