using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] TMP_Text valueText;
    [SerializeField] Vector3 driftVelocity = new Vector3(0f, 1.25f, 0f);
    [SerializeField] float lifetime = 0.8f;

    Color _baseColor = Color.white;
    float _elapsedTime;

    public void Initialize(int value, Color color)
    {
        _baseColor = color;

        if (valueText != null)
        {
            valueText.text = $"-{value}";
            valueText.color = color;
        }
    }

    void Update()
    {
        _elapsedTime += Time.deltaTime;
        transform.position += driftVelocity * Time.deltaTime;

        if (valueText != null)
        {
            Color color = _baseColor;
            color.a = Mathf.Lerp(1f, 0f, Mathf.Clamp01(_elapsedTime / lifetime));
            valueText.color = color;
        }

        if (_elapsedTime >= lifetime)
            Destroy(gameObject);
    }
}
