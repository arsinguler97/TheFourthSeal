using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet I { get; private set; }

    private int _wallet = 0;

    [SerializeField] private AudioCue goldSFX;

    [Header("Gain/Loss Popup")]
    [SerializeField] private TextMeshProUGUI textTotal;
    [SerializeField] private TextMeshProUGUI textChange;
    [SerializeField] private GameObject panel;
    [SerializeField] private string sceneNameToHidePersistentWalletUI = "ShopScene";

    public int CurrentGold => _wallet;


    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;

        ResetWallet();
    }

    private void OnDestroy()
    {
        if (I == this)
            SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public void ResetWallet()
    {
        _wallet = 0;

        if (textTotal != null)
            textTotal.text = _wallet.ToString();

        if (textChange != null)
        {
            textChange.text = "";
            textChange.gameObject.SetActive(false);
        }

        //if (panel != null)
        //    panel.SetActive(false);
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        RefreshPersistentWalletUIVisibility(scene.name);
    }

    void RefreshPersistentWalletUIVisibility(string sceneName)
    {
        if (panel == null)
            return;

        bool shouldShowPanel = !string.Equals(
            sceneName,
            sceneNameToHidePersistentWalletUI,
            System.StringComparison.OrdinalIgnoreCase);

        panel.SetActive(shouldShowPanel);
    }

    public void AddToWallet(int amount)
    {
        _wallet += amount;
        StartCoroutine(UpdateDisplay(amount, true));

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound(goldSFX);
    }

    // For Shop to Display whether the Item is Purchaseable or Not (ex: Greyed out if Below is False)
    public bool SufficientFunds(int amount) { return _wallet >= amount; }

    // For when the Player tries to Buy an Item from the Shop - should theoretically always be true if you properly use the above on Shop Buttons
    public bool MakeTransaction(int amount)
    {
        if (!SufficientFunds(amount)) return false;

        _wallet -= amount;
        StartCoroutine(UpdateDisplay(amount, false));

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound(goldSFX);

        return true;
    }


    private IEnumerator UpdateDisplay(int amount, bool wasAddition)
    {
        if (textTotal != null)
            textTotal.text = (_wallet + (wasAddition ? -amount : amount)).ToString();

        if (textChange != null)
        {
            textChange.text = wasAddition ? "+" + amount : "-" + amount.ToString();
            textChange.gameObject.SetActive(true);
        }

        //if (panel != null)
        //    panel.SetActive(true);

        yield return new WaitForSeconds(1);

        if (textTotal != null)
            textTotal.text = _wallet.ToString();

        if (textChange != null)
        {
            textChange.gameObject.SetActive(false);
            textChange.text = "";
        }

        yield return new WaitForSeconds(2);

        //if (panel != null)
        //    panel.SetActive(false);
    }
}
