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
        if (!_diceDictionary.ContainsKey(numberOfSides))
        {
            Debug.LogError("No Valid Dice with " + numberOfSides + " sides!");
            return;
        }

        AudioManager.Instance.PlaySound(diceRollSFX);
        StartCoroutine(RollDiceCoroutine(numberOfSides, diceCanvas.GetImage(), false));
    }

    public void RollMultiDice(List<(int numberOfSides, DiceCanvas diceCanvas)> diceList)
    {
        _rollsCompleted.Clear();

        foreach (var dice in diceList)
        {
            _rollsPending++;
            StartCoroutine(RollDiceCoroutine(dice.numberOfSides, dice.diceCanvas.GetImage(), true));
        }
    }

    private IEnumerator RollDiceCoroutine(int numberOfSides, Image diceImage, bool isMulti)
    {
        int randomDiceSide = 0;

        // Loop to switch dice sides randomly
        // before final side appears. 20 itterations here.
        for (int i = 0; i <= 20; i++)
        {
            randomDiceSide = Random.Range(0, _diceDictionary[numberOfSides].Length);

            diceImage.sprite = _diceDictionary[numberOfSides][randomDiceSide];

            yield return new WaitForSeconds(0.05f);
        }


        if (isMulti)
        {
            _rollsPending--;
            _rollsCompleted.Add(randomDiceSide + 1);

            if (_rollsPending == 0)
            {
                OnMultiDiceRollCompleted?.Invoke(_rollsCompleted);
            }
        }
        else
        {
            OnDiceRollCompleted?.Invoke(randomDiceSide + 1);
        }

        Debug.Log(randomDiceSide + 1);
    }
}
