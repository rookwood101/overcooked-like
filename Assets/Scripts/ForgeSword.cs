using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ForgeSword : MonoBehaviour
{
    Transform swordTransform;
    void Start()
    {
        swordTransform = GetComponent<Transform>();
        BasicShape(16.712f, 0.084402f, 0.70898f); // https://regenyei.com/product/arming-sword-7/#blade
    }

    private async void BasicShape(float length, float thickness, float width) {
        await Task.Delay(2000);
        var delay = 1000;
        var margin = 0.01f;
        var maxStrength = 0.5f;
        var idealScale = new Vector3(length, thickness, width);
        while ((idealScale - swordTransform.localScale).magnitude > margin)
        {
            CalculateHammerPlane(idealScale, maxStrength);
            await Task.Delay(delay);
        }
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
    private Vector3 SmallestAxis(Vector3 vec) {
        if (vec.x < vec.y && vec.x < vec.z) {
            return Vector3.right;
        }
        if (vec.y < vec.x && vec.y < vec.z) {
            return Vector3.up;
        }
        return Vector3.forward;
    }
    private Vector3 Abs(Vector3 vec) {
        return new Vector3(Mathf.Abs(vec.x), Mathf.Abs(vec.y), Mathf.Abs(vec.z));
    }
}
