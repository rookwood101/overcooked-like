using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

public class PlayerActionController : MonoBehaviour
{
    private HashSet<GameObject> collidingObjects = new HashSet<GameObject>();

    private string inventory = null;

    // have inventory per game object of interest
    // should scrap be modeled as an infinite inventory?
    // think about using a state machine model
    // if we go that route we'll need someway to update the game to match the state

    private void InventoryAdd(string item) {
        Assert.IsNull(inventory);
        inventory = item;
        Debug.Log("Inventory: " + inventory);
    }

    private void InventoryRemove(string item) {
        Assert.AreEqual(item, inventory);
        inventory = null;
        Debug.Log("Inventory empty");
    }

    public void OnPickUpPutDownButton(InputAction.CallbackContext context) {
        if (context.action.triggered && collidingObjects.Count == 1 ) {
            if (collidingObjects.Single().tag == "scrap") {
                if (inventory == null) {
                    InventoryAdd("scrap");
                } else {
                    InventoryRemove(inventory);
                }
            }
        }
    }

    public void OnActivateButton(InputAction.CallbackContext context) {
        if (context.action.triggered && collidingObjects.Count == 1 && collidingObjects.Single().tag == "anvil") {
            Debug.Log("Activated anvil");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        collidingObjects.Add(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        collidingObjects.Remove(other.gameObject);
    }
}
