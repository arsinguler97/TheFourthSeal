using UnityEngine;
using UnityEngine.UI;

public class DiceCanvas : MonoBehaviour
{
    [SerializeField] private Image image;


    public Image GetImage()
    {
        return image;
    }
}
