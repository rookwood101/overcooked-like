using UnityEngine;

public class Triggerable : MonoBehaviour
{
    private float triggerScaleMultiplier = 1.4f; // Multiplier for the trigger's scale compared to the parent collider

    private void Start()
    {
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
}