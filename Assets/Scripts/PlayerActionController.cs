using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerActionController : MonoBehaviour
{
    // constants
    private static readonly Dictionary<string, string> unlimitedInventoryStations = new Dictionary<string, string>() {
        {"stock_pile", "ingot"}
    };
    private static readonly Dictionary<string, Dictionary<string, string>> transformationStations = new Dictionary<string, Dictionary<string, string>>() {
        {"forge", new Dictionary<string, string>() {
            {"stock", "hot_stock"},
            {"ingot", "ingot"},
        }},
        {"anvil", new Dictionary<string, string>() {
            {"hot_stock", "hot_shaped_stock"},
            {"ingot", "ingot"}
        }},
        {"water_bath", new Dictionary<string, string>() {
            {"hot_shaped_stock", "tempered_shaped_stock"},
            {"hot_stock", "stock"},
            {"ingot", "ingot"},
        }},
        {"grind_stone", new Dictionary<string, string>() {
            {"tempered_shaped_stock", "blade"},
            {"ingot", "blade"},
        }}
    };
    private static readonly HashSet<string> autoTransformStations = new HashSet<string>() {
        "forge", "water_bath"
    };
    private GameObject progressbarPrefab;
    private Camera mainCamera;
    private GameObject canvas;

    // semi state
    private HashSet<GameObject> collidingObjects = new HashSet<GameObject>();
    
    // state
    public static Dictionary<GameObject, string> inventories = new Dictionary<GameObject, string>();
    private static Dictionary<GameObject, TransformAttempt> stationTransformAttempts = new Dictionary<GameObject, TransformAttempt>();

    // think about using a state machine model
    // if we go that route we'll need someway to update the game to match the state
    // TODO: station class

    private void Start() {
        progressbarPrefab = Resources.Load<GameObject>("Progress Bar");
        mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        canvas = GameObject.Find("Canvas");
        if (Application.isEditor) {
            SetInventory(GameObject.Find("fi_vil_forge_coolingbath_large2"), "blade");
        }
    }

    public void SetInventory(GameObject inventory, string item) {
        inventories[inventory] = item;

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
        var instance = Instantiate(loadedPrefab, inventory.transform);
        instance.transform.localPosition = Vector3.up;
    }

    public void SetInventory(GameObject inventory, string item, GameObject itemGO) {
        inventories[inventory] = item;

        // Delete existing inventory item game object
        Transform[] childTransforms = inventory.transform.GetComponentsInChildren<Transform>();
        foreach (Transform childTransform in childTransforms)
        {
            if (childTransform.CompareTag("inventory_item"))
            {
                Destroy(childTransform.gameObject);
            }
        }

        itemGO.SetActive(true);
        itemGO.transform.SetParent(inventory.transform);
        itemGO.transform.localPosition = Vector3.up;
    }

    public (string, GameObject) PopInventory(GameObject inventory) {
        Transform[] childTransforms = inventory.transform.GetComponentsInChildren<Transform>();
        foreach (Transform childTransform in childTransforms)
        {
            if (childTransform.CompareTag("inventory_item"))
            {
                childTransform.SetParent(null);
                childTransform.gameObject.SetActive(false);
                var item = inventories[inventory];
                inventories[inventory] = null;
                return (item, childTransform.gameObject);
            }
        }
        throw new System.Exception("no inventory item??"); // unexpected TODO
        
    }

    private string GetInventory(GameObject inventory) {
        return inventories.GetValueOrDefault(inventory);
    }

    public void OnPickUpPutDownButton(InputAction.CallbackContext context) {
        if (context.action.triggered && collidingObjects.Count == 1 ) {
            var station = collidingObjects.Single();
            if (GetInventory(gameObject) == null) {
                // the player is holding nothing. Pick up station inventory or station default
                if (GetInventory(station) == null) {
                    SetInventory(gameObject, unlimitedInventoryStations.GetValueOrDefault(station.tag));
                } else {
                    var (item, itemGO) = PopInventory(station);
                    SetInventory(gameObject, item, itemGO);
                }

                CancelTransformingForStation(station);
            } else if (GetInventory(station) == null) {
                // the player is holding something. Put it down
                var (item, itemGO) = PopInventory(gameObject);
                SetInventory(station, item, itemGO);

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
                    if (stationInventory == "ingot" && station.tag == "anvil") {
                        Ingot ingot = station.GetComponentInChildren<Ingot>();
                        ingot.BasicShape();
                    } else {
                        StartTransforming(station, true);
                    }
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
            var progressbar = Instantiate(progressbarPrefab, canvas.transform);
            var progressbarTransform = progressbar.GetComponent<RectTransform>();
            progressbarTransform.anchoredPosition = mainCamera.WorldToScreenPoint(station.transform.position); // z is discarded
            var slider = progressbar.GetComponent<Slider>();
            while (attempt.elapsed < 5) {
                slider.value = attempt.elapsed / 5f;
                await Task.Yield(); // TODO: may have to use https://github.com/Cysharp/UniTask
                attempt.elapsed += (Time.time - attempt.lastFrameTime) * attempt.players.Count; // more players makes it go faster
                attempt.lastFrameTime = Time.time;
                if (attempt.cancelled) {
                    Destroy(progressbar);
                    return;
                }
            }
            Debug.Log("Transforming complete!");
            stationTransformAttempts.Remove(station);
            if (from == "ingot") {
                Ingot ingot = station.GetComponentInChildren<Ingot>();
                if (station.tag == "forge") {
                    ingot.Heat();
                } if (station.tag == "water_bath") {
                    ingot.Cool();
                } if (station.tag == "grind_stone") {
                    if (ingot.IsFinalShape()) {
                        SetInventory(station, to);
                    }
                }
            } else {
                SetInventory(station, to);
            }
            Destroy(progressbar);
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