using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet I { get; private set; }

    private int _wallet = 0;

    [SerializeField] private AudioCue goldSFX;

    [SerializeField] private TextMeshProUGUI textTotal;
    [SerializeField] private TextMeshProUGUI textChange;
    [SerializeField] private GameObject panel;


    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        ResetWallet();
    }

    public void ResetWallet()
    {
        _wallet = 0;

        textTotal.text = _wallet.ToString();
        textChange.text = "";
        panel.SetActive(false);
    }

    public void AddToWallet(int amount)
    {
        _wallet += amount;
        StartCoroutine(UpdateDisplay(amount, true));
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
        AudioManager.Instance.PlaySound(goldSFX);

        return true;
    }


    private IEnumerator UpdateDisplay(int amount, bool wasAddition)
    {
        textTotal.text = (_wallet + (wasAddition ? -amount : amount)).ToString();
        textChange.text = wasAddition ? "+" + (amount) : "-" + (amount).ToString();

        panel.SetActive(true);

        yield return new WaitForSeconds(1);

        textTotal.text = _wallet.ToString();
        textChange.text = "";

        yield return new WaitForSeconds(2);

        panel.SetActive(false);
    }
}
