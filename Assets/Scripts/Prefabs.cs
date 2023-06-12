using UnityEngine;

public class Prefabs : MonoBehaviour
{
    public static GameObject progressBar;
    public static GameObject speechBubbleBad;
    public static GameObject score;
    public static GameObject ingot;
    public static Material metal;
    public static Material hotMetal;
    public static AudioClip anvilSound;
    public static AudioSource audioSource;

    void Awake()
    {
        progressBar = Resources.Load<GameObject>("Progress Bar");
        speechBubbleBad = Resources.Load<GameObject>("Speech Bubble Bad");
        score = Resources.Load<GameObject>("Score");
        ingot = Resources.Load<GameObject>("Ingot");
        metal = Resources.Load<Material>("Metal");
        hotMetal = Resources.Load<Material>("Hot Metal");
        anvilSound = Resources.Load<AudioClip>("274846__mrauralization__anvil");
        audioSource = GetComponent<AudioSource>();
    }
}
