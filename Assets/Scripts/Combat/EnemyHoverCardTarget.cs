using UnityEngine;

[RequireComponent(typeof(EnemyUnit))]
[RequireComponent(typeof(Collider2D))]
public class EnemyHoverCardTarget : MonoBehaviour
{
    EnemyUnit _enemyUnit;

    public EnemyUnit EnemyUnit => _enemyUnit;

    void Awake()
    {
        _enemyUnit = GetComponent<EnemyUnit>();
    }
}
