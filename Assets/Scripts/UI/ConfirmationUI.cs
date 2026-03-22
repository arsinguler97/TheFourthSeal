using UnityEngine;
using UnityEngine.UI;
using System;

public class ConfirmationUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private Action<bool> callback;


    public void Show(Action<bool> resultCallback)
    {
        panel.SetActive(true);
        callback = resultCallback;

        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();

        yesButton.onClick.AddListener(() => Respond(true));
        noButton.onClick.AddListener(() => Respond(false));
    }

    private void Respond(bool result)
    {
        panel.SetActive(false);
        callback?.Invoke(result);
    }
}