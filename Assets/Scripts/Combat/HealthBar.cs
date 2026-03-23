using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    private float _maxHealth;
    private float _currentHealth;

    [SerializeField] private Image healthBar;


    public void SetMaxHealth(float maxHealth) { _maxHealth = maxHealth; }

    public void SetCurrentHealth(float health)
    {
        _currentHealth = health;
        float newFillAmount = _currentHealth / _maxHealth;
        healthBar.fillAmount = newFillAmount;

        if (newFillAmount >= 0.7f)
            healthBar.color = Color.green;
        else if (newFillAmount >= 0.35f)
            healthBar.color = Color.yellow;
        else
            healthBar.color = Color.red;
    }
}
