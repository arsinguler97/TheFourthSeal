using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Combat/Action Definition")]
public class ActionDefinitionSO : ScriptableObject
{
    public ActionType actionType;
    public int actionCost = 1;
}
