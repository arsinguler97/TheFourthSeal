using System;
using System.Collections;
using UnityEngine;

public class ProjectileVisual : MonoBehaviour
{
    [SerializeField] float moveSpeedUnitsPerSecond = 8f;

    Action _onArrived;

    public void Initialize(Vector3 startWorldPosition, Vector3 endWorldPosition, Action onArrived)
    {
        transform.position = startWorldPosition;
        RotateToward(endWorldPosition - startWorldPosition);
        _onArrived = onArrived;
        StartCoroutine(FlyRoutine(endWorldPosition));
    }

    IEnumerator FlyRoutine(Vector3 endWorldPosition)
    {
        while (Vector3.Distance(transform.position, endWorldPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                endWorldPosition,
                moveSpeedUnitsPerSecond * Time.deltaTime);
            yield return null;
        }

        transform.position = endWorldPosition;
        _onArrived?.Invoke();
        Destroy(gameObject);
    }

    void RotateToward(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // The projectile art is authored facing up, so convert from right-facing angle space.
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }
}
