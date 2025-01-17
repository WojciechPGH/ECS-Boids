using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Boid : MonoBehaviour
{

    public void Init(float3 position, float2 direction)
    {
        transform.SetPositionAndRotation(position, GetDirection(direction));
    }

    private Quaternion GetDirection(float2 velocity)
    {
        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360;
        return Quaternion.Euler(0f, 0f, angle - 90f);
    }
}
