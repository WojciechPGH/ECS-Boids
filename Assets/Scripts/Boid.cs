using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{
    private Vector3 _acceleration;
    private Vector3 _velocity;

    public Vector3 Velocity => _velocity;

    public void Init(Vector3 position, Vector3 direction, float speed)
    {
        _velocity = direction * speed;
        transform.SetPositionAndRotation(position, GetDirection());
        _acceleration = Vector3.zero;
    }

    public void AddForce(Vector3 force, float forceRatio)
    {
        forceRatio = Mathf.Clamp01(forceRatio);
        _acceleration += force * forceRatio;
    }


    public void UpdateBoid(float maxSpeed)
    {
        Quaternion rotation = GetDirection();
        transform.rotation = rotation;
        _velocity += _acceleration * maxSpeed;
        if (_velocity.magnitude > maxSpeed)
        {
            _velocity = _velocity.normalized * maxSpeed;
        }
        //Vector2 desiredSpeed = _acceleration.normalized * _speed - _velocity;
        Vector3 translate = Time.deltaTime * _velocity;
        transform.position += translate;
        _acceleration = Vector2.zero;
    }

    private Quaternion GetDirection()
    {
        float angle = Mathf.Atan2(_velocity.y, _velocity.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360;
        return Quaternion.Euler(0f, 0f, angle - 90f);
    }
}
