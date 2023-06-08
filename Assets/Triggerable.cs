using System.Collections.Generic;
using UnityEngine;

public class Triggerable : MonoBehaviour
{
    private static readonly float triggerScaleMultiplier = 1.4f; // Multiplier for the trigger's scale compared to the parent collider
    private static readonly Color highlightColor = new Color(0.2f, 0.2f, 0.2f, 1);

    private HashSet<GameObject> collidingObjects = new HashSet<GameObject>();
    private new Renderer renderer;

    private void Start()
    {
        renderer = GetComponent<Renderer>();

        // Get the current game object's capsule collider
        CapsuleCollider parentCollider = GetComponent<CapsuleCollider>();

        if (parentCollider == null)
        {
            Debug.LogError("No capsule collider found on the parent object.");
            return;
        }

        // Add a second capsule collider component
        CapsuleCollider triggerCollider = gameObject.AddComponent<CapsuleCollider>();

        triggerCollider.isTrigger = true;
        triggerCollider.direction = parentCollider.direction;
        triggerCollider.center = parentCollider.center;
        // Set the trigger collider's size to be larger
        triggerCollider.radius = parentCollider.radius * triggerScaleMultiplier;
        triggerCollider.height = parentCollider.height * triggerScaleMultiplier;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collidingObjects.Count == 0) {
            // Highlight
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", highlightColor);
        }
        collidingObjects.Add(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        collidingObjects.Remove(other.gameObject);
        if (collidingObjects.Count == 0) {
            // Unhighlight
            renderer.material.DisableKeyword("_EMISSION");
        }
    }
}