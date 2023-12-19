using UnityEngine;

public class Ingot : MonoBehaviour
{
    private Transform swordTransform;
    private new Renderer renderer;
    private bool isHot = false;
    private const float MAX_STRENGTH = 0.1f;
    private const float MARGIN = 0.01f;

    private const float DEFAULT_LENGTH = 16.712f;
    private const float DEFAULT_THICKNESS = 0.084402f;
    private const float DEFAULT_WIDTH = 0.70898f;

    void Start()
    {
        swordTransform = GetComponent<Transform>();
        renderer = GetComponent<Renderer>();
    }

    public void Heat() {
        isHot = true;
        renderer.material = Prefabs.hotMetal;
    }
    public void Cool() {
        isHot = false;
        renderer.material = Prefabs.metal;
    }

    // dimensions from https://regenyei.com/product/arming-sword-7/#blade
    public void BasicShape(float length = DEFAULT_LENGTH, float thickness = DEFAULT_THICKNESS, float width = DEFAULT_WIDTH) {
        var idealScale = new Vector3(length, thickness, width);
        if (!IsFinalShape(length, thickness, width)) // consider removing
        {
            if (isHot) {
                CalculateHammerPlane(idealScale, MAX_STRENGTH);
            }
            // TODO: credit https://freesound.org/people/MrAuralization/sounds/274846/
            Prefabs.audioSource.PlayOneShot(Prefabs.anvilSound);
        }
    }

    public bool IsFinalShape(float length = DEFAULT_LENGTH, float thickness = DEFAULT_THICKNESS, float width = DEFAULT_WIDTH) {
        var idealScale = new Vector3(length, thickness, width);
        return (idealScale - swordTransform.localScale).magnitude <= MARGIN;
    }

    private void CalculateHammerPlane(Vector3 idealScale, float maxStrength)
    {
        // prefer to hit metal on plane of axis with most to gain absolute and smallest axis of remaining axes
        var diffToIdeal = idealScale - swordTransform.localScale;
        var firstAxis = BiggestAxis(diffToIdeal);
        var secondAxis = BiggestAxis((float.MinValue * firstAxis) + diffToIdeal);
        var plane = Vector3.one - firstAxis - secondAxis;
        // strength to hit should result in bringing us closer to diffToIdeal.magnitude == 0
        // probably that means strength should be proportional to biggest diffToIdeal
        var strengthLimitAxis = firstAxis;
        var strength = Mathf.Min(Vector3.Scale(strengthLimitAxis, diffToIdeal).magnitude, maxStrength); // perhaps maxStrength is altered by temperature
        Hammer(plane, strength); // TODO: introduce some jitter into strength - based on temperature and rhythm
    }

    private void Hammer(Vector3 contractionAxis, float strength) {
        Vector3 expansion = strength * (Vector3.one - contractionAxis) + Vector3.Scale(Vector3.one - contractionAxis, swordTransform.localScale);
        Vector3 temp = expansion + contractionAxis; // add a 1 in so that when we multiply together it doesn't affect result
        var contraction = 1 / (temp.x * temp.y * temp.z) * contractionAxis; // preserve volume - volume should always equal 1
        swordTransform.localScale = expansion + contraction;
    }

    private Vector3 BiggestAxis(Vector3 vec) {
        if (vec.x > vec.y && vec.x > vec.z) {
            return Vector3.right;
        }
        if (vec.y > vec.x && vec.y > vec.z) {
            return Vector3.up;
        }
        return Vector3.forward;
    }
}
