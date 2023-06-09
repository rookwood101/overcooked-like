using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class NpcController : MonoBehaviour
{
    private GameObject npcPrefab;
    private GameObject speechBubblePrefab;
    private GameObject checkout;
    private GameObject spawn;
    private GameObject canvas;
    private Camera mainCamera;
    
    // level length ~5 min
    // level 1 - 3 min
    // add a new order at the start of the level
    // when an order is completed, spawn a new one and reset timer
    // spawn a new order every 35 seconds (8 seconds in 1-1 overcooked 2)
    // minimum time to make a sword is currently 20 seconds (~4 seconds in 1-1 overcooked 2)
    // order failed after 1 minute

    void Start()
    {
        npcPrefab = Resources.Load<GameObject>("NPC");
        speechBubblePrefab = Resources.Load<GameObject>("Speech Bubble");
        checkout = GameObject.Find("NPC Goal Location");
        spawn = GameObject.Find("NPC Spawn Location");
        canvas = GameObject.Find("Canvas");
        mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();

        BeginLevel(25, 60, 3*60);
    }

    private async void BeginLevel(float orderGap, float orderFailTime, float levelTime) {
        var levelStartTime = Time.time;
        var levelEndTime = levelStartTime + levelTime;
        var levelProgressBar = Instantiate(Prefabs.progressBar, canvas.transform).GetComponent<Slider>();
        var currentOrderGap = 0f;
        var levelContext = new LevelContext();

        var levelProgressBarTransform = levelProgressBar.GetComponent<RectTransform>();
        levelProgressBarTransform.anchorMin = new Vector2(0.5f, 1);
        levelProgressBarTransform.anchorMax = new Vector2(0.5f, 1);
        levelProgressBarTransform.anchoredPosition = new Vector2(0, -20);
        levelProgressBarTransform.sizeDelta = new Vector2(100, 30);

        // initial order
        TravelAndBuy(levelContext);

        while (Time.time <= levelEndTime) {
            currentOrderGap += Time.deltaTime;
            levelProgressBar.value += Time.deltaTime / levelTime;

            if (currentOrderGap > orderGap || levelContext.outstandingOrders == 0) {
                currentOrderGap = 0;
                TravelAndBuy(levelContext);
            }

            await Task.Yield();
        }
    }

    private async void TravelAndBuy(LevelContext levelContext) {
        levelContext.outstandingOrders++;
        var npcGO = Instantiate(npcPrefab, spawn.transform.position, Quaternion.identity);
        var npc = npcGO.GetComponent<PlayerMovementController>();
    
        await TravelTo(npc, checkout);
        // then show speech bubble
        var speechBubble = Instantiate(speechBubblePrefab, canvas.transform);
        var speechBubbleTransform = speechBubble.GetComponent<RectTransform>();
        speechBubbleTransform.anchoredPosition = mainCamera.WorldToScreenPoint(npc.transform.position) + new Vector3(0, 75, 0); // z is discarded

        // await receiving item from player
        while (PlayerActionController.inventories.GetValueOrDefault(npc.gameObject) != "blade") {
            await Task.Yield();
        }
        
        // go back to spawn
        Destroy(speechBubble);
        levelContext.outstandingOrders--;
        levelContext.completedOrders++;
        await TravelTo(npc, spawn);
        Destroy(npcGO);
    }

    private async Task TravelTo(PlayerMovementController npc, GameObject goal) {
        var goalVector = GoalVector(npc, goal);
        while (goalVector.magnitude > 0.5) {
            npc.OnMove(goalVector.normalized);
            await Task.Yield();
            goalVector = GoalVector(npc, goal);
        }
        // TODO: if we hit an obstacle, stop
        npc.OnMove(Vector2.zero);
    }

    private Vector2 GoalVector(PlayerMovementController npc, GameObject goal) {
        var goalVector = goal.transform.position - npc.transform.position;
        return new Vector2(goalVector.x, goalVector.z);
    }
}

public record LevelContext {
    public bool finished {get; set;} = false;
    public int outstandingOrders {get; set;} = 0;
    public int completedOrders {get; set;} = 0;
};