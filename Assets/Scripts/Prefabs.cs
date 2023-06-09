using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Prefabs : MonoBehaviour
{
    public static GameObject progressBar;

    void Awake()
    {
        progressBar = Resources.Load<GameObject>("Progress Bar");
    }
}
