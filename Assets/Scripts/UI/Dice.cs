using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;



// based on Credits: https://www.youtube.com/watch?v=JgbJZdXDNtg



public class Dice : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private Sprite[] diceSides;
    [SerializeField] private AudioCue diceRollSFX;
    private Image image;
    private int diceFaceValue = 0;


	private void Awake () 
    {
        image = GetComponent<Image>();
	}

    // If you left click over the dice then RollTheDice coroutine is started
    public void OnPointerDown(PointerEventData eventData)
    {
        //AudioManager.Instance.PlaySound(diceRollSFX);
        //StartCoroutine(RollDiceCoroutine());
    }

    public Task<int> RollDice()
    {
        TaskCompletionSource<int> taskCompleted = new TaskCompletionSource<int>();

        image.raycastTarget = false;
        AudioManager.Instance.PlaySound(diceRollSFX);
        StartCoroutine(RollDiceCoroutine(taskCompleted));

        return taskCompleted.Task;
    }

    // Coroutine that rolls the dice
    private IEnumerator RollDiceCoroutine(TaskCompletionSource<int> taskCompleted)
    {
        // Variable to contain random dice side number.
        // It needs to be assigned. Let it be 0 initially
        int randomDiceSide = 0;

        // Loop to switch dice sides ramdomly
        // before final side appears. 20 itterations here.
        for (int i = 0; i <= 20; i++)
        {
            // Pick up random value from 0 to 5 (All inclusive)
            randomDiceSide = Random.Range(0, 5);

            // Set sprite to upper face of dice from array according to random value
            image.sprite = diceSides[randomDiceSide];

            // Pause before next itteration
            yield return new WaitForSeconds(0.05f);
        }

        // Assigning final side so you can use this value later in your game
        // for player movement for example
        diceFaceValue = randomDiceSide + 1;

        // Show final dice value in Console
        Debug.Log(diceFaceValue);
        taskCompleted.SetResult(diceFaceValue);
    }
}
