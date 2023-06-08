using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class NpcController : MonoBehaviour
{
    private GameObject npcPrefab;
    private GameObject goal;
    private GameObject spawn;

    private PlayerMovementController npc;

    void Start()
    {
        npcPrefab = Resources.Load<GameObject>("NPC");
        goal = GameObject.Find("NPC Goal Location");
        spawn = GameObject.Find("NPC Spawn Location");

        var npcGO = Instantiate(npcPrefab, spawn.transform.position, Quaternion.identity);
        npc = npcGO.GetComponent<PlayerMovementController>();
    }

    void Update()
    {
        var goalVector = goal.transform.position - npc.transform.position;
        var goalVector2 = new Vector2(goalVector.x, goalVector.z);
        if (goalVector2.magnitude > 0.5) {
            npc.OnMove(goalVector2.normalized);
        } else {
            npc.OnMove(Vector2.zero);
        }
        //TODO: move to start and use task.yield
        // then show speech bubble
    }
}
