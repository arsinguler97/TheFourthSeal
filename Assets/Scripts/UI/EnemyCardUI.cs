using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyCardUI : MonoBehaviour
{
    public static EnemyCardUI Instance { get; private set; }

    [SerializeField] GameObject cardRoot;
    [SerializeField] TMP_Text nameText;
    [SerializeField] Image portraitImage;
    [SerializeField] TMP_Text attackText;
    [SerializeField] TMP_Text healthText;
    [SerializeField] TMP_Text flavourText;
    [SerializeField] TMP_Text strengthText;
    [SerializeField] TMP_Text rangeText;
    [SerializeField] TMP_Text speedText;
    [SerializeField] TMP_Text defenceText;

    EnemyUnit _currentEnemy;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SetCardVisible(false);
    }

    void Update()
    {
        UpdateHoveredEnemy();

        if (_currentEnemy == null || !_currentEnemy || !_currentEnemy.IsAlive)
        {
            _currentEnemy = null;
            SetCardVisible(false);
            return;
        }

        RefreshCard();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ShowEnemy(EnemyUnit enemyUnit)
    {
        _currentEnemy = enemyUnit;
        SetCardVisible(_currentEnemy != null);
        RefreshCard();
    }

    public void HideEnemy(EnemyUnit enemyUnit)
    {
        if (_currentEnemy != enemyUnit)
            return;

        _currentEnemy = null;
        SetCardVisible(false);
    }

    void RefreshCard()
    {
        if (_currentEnemy == null)
            return;

        if (nameText != null)
            nameText.text = _currentEnemy.DisplayName;

        if (portraitImage != null)
        {
            Sprite cardSprite = _currentEnemy.EnemyDefinition != null && _currentEnemy.EnemyDefinition.worldSprite != null
                ? _currentEnemy.EnemyDefinition.worldSprite
                : _currentEnemy.GetTurnOrderSprite();
            portraitImage.sprite = cardSprite;
            portraitImage.enabled = cardSprite != null;
        }

        if (attackText != null)
            attackText.text = _currentEnemy.AttackDieSize.ToString();

        if (healthText != null)
            healthText.text = _currentEnemy.CurrentHealth.ToString();

        if (flavourText != null)
            flavourText.text = _currentEnemy.EnemyDefinition != null ? _currentEnemy.EnemyDefinition.description : string.Empty;

        if (strengthText != null)
            strengthText.text = _currentEnemy.Stats.Strength.ToString();

        if (rangeText != null)
            rangeText.text = _currentEnemy.Stats.Range.ToString();

        if (speedText != null)
            speedText.text = _currentEnemy.Stats.Speed.ToString();

        if (defenceText != null)
            defenceText.text = _currentEnemy.Stats.Defence.ToString();
    }

    void SetCardVisible(bool isVisible)
    {
        if (cardRoot != null)
            cardRoot.SetActive(isVisible);
        else
            gameObject.SetActive(isVisible);
    }

    void UpdateHoveredEnemy()
    {
        if (Camera.main == null)
            return;

        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;

        Collider2D hoveredCollider = Physics2D.OverlapPoint(mouseWorldPosition);
        EnemyHoverCardTarget hoveredTarget = hoveredCollider != null
            ? hoveredCollider.GetComponentInParent<EnemyHoverCardTarget>()
            : null;

        EnemyUnit hoveredEnemy = hoveredTarget != null ? hoveredTarget.EnemyUnit : null;
        if (hoveredEnemy != null && hoveredEnemy.IsAlive)
        {
            if (_currentEnemy != hoveredEnemy)
                ShowEnemy(hoveredEnemy);

            return;
        }

        if (_currentEnemy != null)
            HideEnemy(_currentEnemy);
    }
}
