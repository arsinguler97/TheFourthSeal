using System.Collections;
using System.Collections.Generic;
using UnityEngine;



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


    public void RollDice(int numberOfSides, DiceCanvas diceCanvas, DiceRollHistory rollHistory = null)
    {
        if (numberOfSides <= 0)// || !_diceDictionary.ContainsKey(numberOfSides))
        {
            Debug.LogError("No Valid Dice with " + numberOfSides + " sides!");
            return;
        }
        else if (diceCanvas == null)
        {
            Debug.LogError("No Valid DiceCanvas!");
            return;
        }

        AudioManager.Instance.PlaySound(diceRollSFX);
        diceCanvas.ResetDisplay();

        StartCoroutine(RollDiceCoroutine(numberOfSides, diceCanvas, false, rollHistory));
    }

    public void RollMultiDice(List<(int numberOfSides, DiceCanvas diceCanvas)> diceList)
    {
        _rollsCompleted.Clear();

        foreach (var dice in diceList)
        {
            _rollsPending++;
            if (dice.diceCanvas != null)
            {
                dice.diceCanvas.ResetDisplay();
                StartCoroutine(RollDiceCoroutine(dice.numberOfSides, dice.diceCanvas, true));
            }
        }
    }

    private IEnumerator RollDiceCoroutine(int numberOfSides, DiceCanvas diceCanvas, bool isMulti, DiceRollHistory rollHistory = null)
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


        if (rollHistory != null)
        {
            float[] weights = rollHistory.GetWeights(numberOfSides);

            float total = 0.0f;

            foreach (float weight in weights)
                total += weight;

            float newRolledValue = Random.value * total;

            for (int i = 0; i < weights.Length; i++)
            {
                if (newRolledValue < weights[i])
                {
                    rolledValue = i + 1;
                    break;
                }

                newRolledValue -= weights[i];
            }

            rolledValue = rollHistory.CheckRollAgainstPrevious(numberOfSides, rolledValue);
            rollHistory.Adjust(numberOfSides, rolledValue);
        }


        if (hasExactDiceSprites)
            diceCanvas.ShowSprite(_diceDictionary[numberOfSides][rolledValue - 1]);
        else
            diceCanvas.ShowOverflowResult(rolledValue);


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
