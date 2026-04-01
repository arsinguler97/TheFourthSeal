using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



// based on Credits: https://www.youtube.com/watch?v=JgbJZdXDNtg




public class DiceManager : MonoBehaviour
{
    public static DiceManager Instance { get; private set; }


    [SerializeField] private Dice[] dices;
    private Dictionary<int, Sprite[]> _diceDictionary = new Dictionary<int, Sprite[]>();

    public event System.Action<int> OnDiceRollCompleted;
    public event System.Action<List<int>> OnMultiDiceRollCompleted;
    private int _rollsPending = 0;
    private List<int> _rollsCompleted = new List<int>();

    [SerializeField] private AudioCue diceRollSFX;
    [SerializeField] private float finalResultHoldDuration = 1f;
    
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;


        foreach (Dice dice in dices)
        {
            _diceDictionary.Add(dice.diceSides.Length, dice.diceSides);
        }
    }


    public void RollDice(int numberOfSides, DiceCanvas diceCanvas)
    {
        if (numberOfSides <= 0 || diceCanvas == null)
        {
            Debug.LogError("No Valid Dice with " + numberOfSides + " sides!");
            return;
        }

        AudioManager.Instance.PlaySound(diceRollSFX);
        diceCanvas.ResetDisplay();
        StartCoroutine(RollDiceCoroutine(numberOfSides, diceCanvas, false));
    }

    public void RollMultiDice(List<(int numberOfSides, DiceCanvas diceCanvas)> diceList)
    {
        _rollsCompleted.Clear();

        foreach (var dice in diceList)
        {
            _rollsPending++;
            if (dice.diceCanvas != null)
                dice.diceCanvas.ResetDisplay();
            StartCoroutine(RollDiceCoroutine(dice.numberOfSides, dice.diceCanvas, true));
        }
    }

    private IEnumerator RollDiceCoroutine(int numberOfSides, DiceCanvas diceCanvas, bool isMulti)
    {
        if (diceCanvas == null)
            yield break;

        bool hasExactDiceSprites = _diceDictionary.ContainsKey(numberOfSides);
        int rolledValue = 1;

        // Loop to switch dice sides randomly
        // before final side appears. 20 itterations here.
        for (int i = 0; i <= 20; i++)
        {
            rolledValue = Random.Range(1, numberOfSides + 1);

            if (hasExactDiceSprites)
                diceCanvas.ShowSprite(_diceDictionary[numberOfSides][rolledValue - 1]);
            else
                diceCanvas.ShowOverflowResult(rolledValue);

            yield return new WaitForSeconds(0.05f);
        }

        yield return new WaitForSeconds(finalResultHoldDuration);


        if (isMulti)
        {
            _rollsPending--;
            _rollsCompleted.Add(rolledValue);

            if (_rollsPending == 0)
            {
                OnMultiDiceRollCompleted?.Invoke(_rollsCompleted);
            }
        }
        else
        {
            OnDiceRollCompleted?.Invoke(rolledValue);
        }

        Debug.Log(rolledValue);
    }
}
