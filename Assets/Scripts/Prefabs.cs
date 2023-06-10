using UnityEngine;

public class Prefabs : MonoBehaviour
{
    public static GameObject progressBar;
    public static GameObject speechBubbleBad;
    public static GameObject score;

    void Awake()
    {
        progressBar = Resources.Load<GameObject>("Progress Bar");
        speechBubbleBad = Resources.Load<GameObject>("Speech Bubble Bad");
        score = Resources.Load<GameObject>("Score");
    }
}
