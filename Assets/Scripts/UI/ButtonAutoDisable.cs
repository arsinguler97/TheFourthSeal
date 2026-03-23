using UnityEngine;
using UnityEngine.UI;

public class ButtonAutoDisable : MonoBehaviour
{
    private Button _button;

    private void Start()
    {
        TryGetComponent(out _button);
        _button.onClick.AddListener(DisableButton);
    }

    public void DisableButton()
    {
        if (_button)
            _button.interactable = false;
    }

    public void EnableButton()
    {
        if (_button)
            _button.interactable = true;
    }
}
