using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Threading;
using System.Reflection;
using System;

public class NpcController : MonoBehaviour
{
    private GameObject npcPrefab;
    private GameObject speechBubblePrefab;
    private GameObject checkout;
    private GameObject spawn;
    private GameObject canvas;
    private TMPro.TMP_Text score;
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
        score = Instantiate(Prefabs.score, canvas.transform).GetComponent<TMPro.TMP_Text>();

        BeginLevel(25, 60, 3*60);
    }

    private async void BeginLevel(float orderGap, float orderFailTime, float levelTime) {
        var levelStartTime = Time.time;
        var levelEndTime = levelStartTime + levelTime;
        var levelProgressBar = Instantiate(Prefabs.progressBar, canvas.transform).GetComponent<Slider>();
        var currentOrderGap = 0f;
        var levelContext = new LevelContext() {
            orderFailTime = orderFailTime,
        };

        var levelProgressBarTransform = levelProgressBar.GetComponent<RectTransform>();
        levelProgressBarTransform.anchorMin = new Vector2(0.5f, 1);
        levelProgressBarTransform.anchorMax = new Vector2(0.5f, 1);
        levelProgressBarTransform.anchoredPosition = new Vector2(0, -20);
        levelProgressBarTransform.sizeDelta = new Vector2(100, 30);

        // initial order
        TravelAndBuy(levelContext, "blade");

        while (Time.time <= levelEndTime) {
            currentOrderGap += Time.deltaTime;
            levelProgressBar.value += Time.deltaTime / levelTime;

            if (currentOrderGap > orderGap || levelContext.outstandingOrders == 0) {
                currentOrderGap = 0;
                TravelAndBuy(levelContext, "blade");
            }

            await Task.Yield();
        }
    }

    private async void TravelAndBuy(LevelContext levelContext, string item) {
        levelContext.outstandingOrders++;
        var npcGO = Instantiate(npcPrefab, spawn.transform.position, Quaternion.identity);
        var npc = npcGO.GetComponent<PlayerMovementController>();
    
        await TravelTo(npc, checkout);
        // then show speech bubble
        var speechBubble = Instantiate(speechBubblePrefab, canvas.transform);
        var speechBubbleTransform = speechBubble.GetComponent<RectTransform>();
        speechBubbleTransform.anchoredPosition = mainCamera.WorldToScreenPoint(npc.transform.position) + new Vector3(0, 75, 0); // z is discarded
    
        var timeoutBar = Instantiate(Prefabs.progressBar, canvas.transform).GetComponent<Slider>();
        var timeoutBarTransform = timeoutBar.GetComponent<RectTransform>();
        timeoutBarTransform.anchoredPosition = mainCamera.WorldToScreenPoint(npc.transform.position); // z is discarded
        timeoutBar.value = 1;
        var waitTimeStart = Time.time;
        // await receiving item from player
        while (PlayerActionController.inventories.GetValueOrDefault(npc.gameObject) != item) {
            if (Time.time > waitTimeStart + levelContext.orderFailTime) {
                Destroy(speechBubble);
                speechBubble = Instantiate(Prefabs.speechBubbleBad, canvas.transform);
                speechBubbleTransform = speechBubble.GetComponent<RectTransform>();
                speechBubbleTransform.anchoredPosition = mainCamera.WorldToScreenPoint(npc.transform.position) + new Vector3(0, 75, 0); // z is discarded
                levelContext.outstandingOrders--;
                await Task.Delay(5000);
                Destroy(timeoutBar.gameObject);
                Destroy(speechBubble);
                await TravelTo(npc, spawn);
                return;
            }
            await Task.Yield();
            timeoutBar.value -= Time.deltaTime / levelContext.orderFailTime;
        }
        
        // go back to spawn
        Destroy(speechBubble);
        Destroy(timeoutBar.gameObject);

        levelContext.outstandingOrders--;
        levelContext.completedOrders++;

        levelContext.score += 50; // cost
        // tip
        var waitTimeLeftProportion = 1f - ((Time.time - waitTimeStart) / levelContext.orderFailTime);
        if (waitTimeLeftProportion > 0.3333f) {
            levelContext.score += 25;
        }
        if (waitTimeLeftProportion > 0.6666f) {
            levelContext.score += 25;
        }
        // TODO: make tip dependant on fulfilling orders in order

        score.text = levelContext.score.ToString();

        await TravelTo(npc, spawn);
        Destroy(npcGO);
    }

    private async Task TravelTo(PlayerMovementController npc, GameObject goal, float timeout = 10) {
        var startTime = Time.time;
        var goalVector = GoalVector(npc, goal);
        while (goalVector.magnitude > 0.5 && Time.time < startTime + timeout) {
            npc.OnMove(goalVector.normalized);
            await Task.Yield();
            goalVector = GoalVector(npc, goal);
        }
        // TODO: if we hit an obstacle, stop. A* or something
        npc.OnMove(Vector2.zero);
    }

    private Vector2 GoalVector(PlayerMovementController npc, GameObject goal) {
        var goalVector = goal.transform.position - npc.transform.position;
        return new Vector2(goalVector.x, goalVector.z);
    }

    // avoids problems with async code continuing to run when play mode stops
    void OnApplicationQuit()
    {
        #if UNITY_EDITOR
            var constructor = SynchronizationContext.Current.GetType().GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] {typeof(int)}, null);
            var newContext = constructor.Invoke(new object[] {Thread.CurrentThread.ManagedThreadId });
            SynchronizationContext.SetSynchronizationContext(newContext as SynchronizationContext);  
        #endif
    }
 
}

public record LevelContext {
    public bool finished {get; set;} = false;
    public int outstandingOrders {get; set;} = 0;
    public int completedOrders {get; set;} = 0;
    public float orderFailTime {get; set;}
    public int score {get; set;} = 0;
};