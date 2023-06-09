using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class NpcController : MonoBehaviour
{
    private GameObject npcPrefab;
    private GameObject speechBubblePrefab;
    private GameObject goal;
    private GameObject spawn;
    private GameObject canvas;
    private Camera mainCamera;

    private PlayerMovementController npc;

    void Start()
    {
        npcPrefab = Resources.Load<GameObject>("NPC");
        speechBubblePrefab = Resources.Load<GameObject>("Speech Bubble");
        goal = GameObject.Find("NPC Goal Location");
        spawn = GameObject.Find("NPC Spawn Location");
        canvas = GameObject.Find("Canvas");
        mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();

        var npcGO = Instantiate(npcPrefab, spawn.transform.position, Quaternion.identity);
        npc = npcGO.GetComponent<PlayerMovementController>();
        TravelAndBuy();
    }

    private async void TravelAndBuy() {
        // travel
        var goalVector = GoalVector();
        while (goalVector.magnitude > 0.5) {
            npc.OnMove(goalVector.normalized);
            await Task.Yield();
            goalVector = GoalVector();
        }
        npc.OnMove(Vector2.zero);
        // then show speech bubble
        var speechBubble = Instantiate(speechBubblePrefab, canvas.transform);
        var speechBubbleTransform = speechBubble.GetComponent<RectTransform>();
        speechBubbleTransform.anchoredPosition = mainCamera.WorldToScreenPoint(npc.transform.position) + new Vector3(0, 75, 0); // z is discarded
    }

    private Vector2 GoalVector() {
        var goalVector = goal.transform.position - npc.transform.position;
        return new Vector2(goalVector.x, goalVector.z);
    }
}
