using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerActionController : MonoBehaviour
{
    // constants
    private static readonly Dictionary<string, string> unlimitedInventoryStations = new Dictionary<string, string>() {
        {"stock_pile", "stock"}
    };
    private static readonly Dictionary<string, Dictionary<string, string>> transformationStations = new Dictionary<string, Dictionary<string, string>>() {
        {"forge", new Dictionary<string, string>() {
            {"stock", "hot_stock"}
        }},
        {"anvil", new Dictionary<string, string>() {
            {"hot_stock", "hot_shaped_stock"}
        }},
        {"water_bath", new Dictionary<string, string>() {
            {"hot_shaped_stock", "tempered_shaped_stock"}
        }},
        {"grind_stone", new Dictionary<string, string>() {
            {"tempered_shaped_stock", "blade"}
        }}
    };
    private static readonly HashSet<string> autoTransformStations = new HashSet<string>() {
        "forge", "water_bath"
    };

    // semi state
    private HashSet<GameObject> collidingObjects = new HashSet<GameObject>();
    
    // state
    private static Dictionary<GameObject, string> inventories = new Dictionary<GameObject, string>();
    private static Dictionary<GameObject, TransformAttempt> stationTransformAttempts = new Dictionary<GameObject, TransformAttempt>();

    // think about using a state machine model
    // if we go that route we'll need someway to update the game to match the state

    private void SetInventory(GameObject inventory, string item) {
        inventories[inventory] = item;
        Debug.Log($"{inventory.tag}: {item}");

        // Delete existing inventory item game object
        Transform[] childTransforms = inventory.transform.GetComponentsInChildren<Transform>();
        foreach (Transform childTransform in childTransforms)
        {
            if (childTransform.CompareTag("inventory_item"))
            {
                Destroy(childTransform.gameObject);
            }
        }

        // Load the prefab for inventory item just above inventory game object
        var loadedPrefab = Resources.Load<GameObject>(item);
        if (loadedPrefab != null)
        {
            var instance = Instantiate(loadedPrefab, inventory.transform);
            instance.transform.localPosition = Vector3.up;
        }
    }

    private string GetInventory(GameObject inventory) {
        return inventories.GetValueOrDefault(inventory);
    }

    public void OnPickUpPutDownButton(InputAction.CallbackContext context) {
        if (context.action.triggered && collidingObjects.Count == 1 ) {
            var station = collidingObjects.Single();
            if (GetInventory(gameObject) == null) {
                // the player is holding nothing. Pick up station inventory or station default
                SetInventory(gameObject, GetInventory(station) ?? unlimitedInventoryStations.GetValueOrDefault(station.tag));
                SetInventory(station, null);

                CancelTransformingForStation(station);
            } else if (GetInventory(station) == null) {
                // the player is holding something. Put it down
                SetInventory(station, GetInventory(gameObject));
                SetInventory(gameObject, null);

                var transformations = transformationStations.GetValueOrDefault(station.tag);
                var stationInventory = GetInventory(station);
                if (transformations != null && stationInventory != null && autoTransformStations.Contains(station.tag)) {
                    var transformation = transformations.GetValueOrDefault(stationInventory);
                    if (transformation != null) {
                        StartTransforming(station, false);
                    }
                }
            } else {
                // the player is holding something but there's nowhere to put it down
                // TODO: sad sound effect?
            }
        }
    }

    public void OnActivateButton(InputAction.CallbackContext context) {
        if (collidingObjects.Count > 1) {
            Debug.Log("Near too many stations, don't know which to activate");    
            return;
        }
        if (collidingObjects.Count == 0) {
            return;
        }

        var depressed = context.ReadValueAsButton();
        var station = collidingObjects.Single();
        if (depressed) {
            var transformations = transformationStations.GetValueOrDefault(station.tag);
            var stationInventory = GetInventory(station);
            if (transformations != null && stationInventory != null && !autoTransformStations.Contains(station.tag)) {
                var transformation = transformations.GetValueOrDefault(stationInventory);
                if (transformation != null) {
                    StartTransforming(station, true);
                }
            }
        } else {
            CancelTransformingForPlayer(gameObject, station);
        }
    }

    private async void StartTransforming(GameObject station, bool requiresPlayer) {
        var from = GetInventory(station);
        var to = transformationStations[station.tag][from];
        var isAttemptCreator = !stationTransformAttempts.ContainsKey(station);
        var attempt = stationTransformAttempts.GetValueOrDefault(station, new TransformAttempt(){
            from = from,
            to = to,
            lastFrameTime = Time.time,
            requiresPlayer = requiresPlayer,
        });
        stationTransformAttempts[station] = attempt;
        attempt.players.Add(gameObject);
        if (isAttemptCreator) {
            Debug.Log($"Transforming {from} to {to} at {station.tag}");
            // TODO: time dependant on job - currently fixed at 5 seconds
            while (attempt.elapsed < 5) {
                Debug.Log($"Transforming {100*(attempt.elapsed / 5f)}%...");
                await Task.Yield(); // TODO: may have to use https://github.com/Cysharp/UniTask
                attempt.elapsed += (Time.time - attempt.lastFrameTime) * attempt.players.Count; // more players makes it go faster
                attempt.lastFrameTime = Time.time;
                if (attempt.cancelled) {
                    return;
                }
            }
            Debug.Log("Transforming complete!");
            stationTransformAttempts.Remove(station);
            SetInventory(station, to);
        }
    }

    private void CancelTransformingForPlayer(GameObject player, GameObject station) {
        foreach (var (transformingStation, attempt) in stationTransformAttempts.ToList()) {
            if (transformingStation == station && attempt.requiresPlayer) {
                attempt.players.Remove(player);
                if (attempt.players.Count == 0) {
                    Debug.Log($"Transformation at {station.tag} cancelled");
                    attempt.cancelled = true;
                    stationTransformAttempts.Remove(station);
                }
            }
        }
    }

    private void CancelTransformingForStation(GameObject station) {
        if (stationTransformAttempts.GetValueOrDefault(station) != null) {
            Debug.Log($"Transformation at {station.tag} cancelled");
            stationTransformAttempts[station].cancelled = true;
            stationTransformAttempts.Remove(station);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        collidingObjects.Add(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        collidingObjects.Remove(other.gameObject);
        CancelTransformingForPlayer(player: gameObject, station: other.gameObject);
    }
}

public record TransformAttempt {
    public HashSet<GameObject> players {get;} = new HashSet<GameObject>();
    public string from {get; init;}
    public string to {get; init;}
    public float lastFrameTime {get; set;}
    public bool requiresPlayer {get; set;}
    public float elapsed {get; set;} = 0;
    public bool cancelled {get; set;} = false;
}

namespace System.Runtime.CompilerServices
{
    public class IsExternalInit
    {

    }
}