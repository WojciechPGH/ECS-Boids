using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Unity.Burst;
using UnityEngine.Jobs;
using Unity.Mathematics;
using Unity.Collections;

[BurstCompile]
public class BoidsController : MonoBehaviour
{
    [SerializeField]
    private GameObject _boidPrefab;
    [SerializeField]
    private float _boidMaxSpeed;
    [SerializeField, Range(0.1f, 1f)]
    private float _boidMinDistance;
    [SerializeField]
    private float _boidAwarenessRadius = 2f;
    [SerializeField]
    private int _numOfBoids = 50;
    [SerializeField]
    private Rect _boundingBox;
    [SerializeField]
    private float _FoVAngle = 60;
    private List<Boid> _boidsList;
    private List<float3> _boidsAcceleration;
    private List<float3> _boidsVelocity;
    private List<float3> _boidsPosition;


    private void Start()
    {
        SetBoundingBox();
        InitBoids();
    }

    private void Update()
    {
        UpdateBoids();
    }

    private void SetBoundingBox()
    {
        float ratio = Camera.main.aspect;
        float camSize = Camera.main.orthographicSize * 0.95f;
        _boundingBox.xMin = -camSize * ratio;
        _boundingBox.xMax = camSize * ratio;
        _boundingBox.yMin = -camSize;
        _boundingBox.yMax = camSize;
    }

    private void InitBoids()
    {
        _boidsList = new List<Boid>(_numOfBoids);
        _boidsAcceleration = new List<float3>(_numOfBoids);
        _boidsVelocity = new List<float3>(_numOfBoids);
        _boidsPosition = new List<float3>(_numOfBoids);

        Boid boid;
        Vector2 pos;
        Vector3 boidPos;
        Vector3 boidsVelocity;

        for (int i = 0; i < _numOfBoids; i++)
        {
            pos = new Vector2(UnityEngine.Random.value, UnityEngine.Random.value);
            boidPos = new Vector3(_boundingBox.xMin + _boundingBox.width * pos.x, _boundingBox.yMin + _boundingBox.height * pos.y);
            boid = Instantiate(_boidPrefab).GetComponent<Boid>();
            pos = UnityEngine.Random.insideUnitCircle.normalized;
            boid.Init(boidPos, pos, _boidMaxSpeed);

            _boidsList.Add(boid);
            boidsVelocity = pos * _boidMaxSpeed;

            _boidsAcceleration.Add(float3.zero);
            _boidsVelocity.Add(boidsVelocity);
            _boidsPosition.Add(boidPos);
        }
    }
    private void UpdateBoids()
    {
        BoidsRules();
        MoveBoids();
    }

    private void BoidsRules()
    {
        float distance;
        float minDistCount, awarenessCount;
        Vector3 boidPos, neighborPos;

        for (int i = 0; i < _boidsList.Count; i++)
        {
            boidPos = _boidsList[i].transform.position;
            Vector3 separation = Vector3.zero;
            Vector3 alignment = Vector3.zero;
            Vector3 cohesion = Vector3.zero;
            minDistCount = 0;
            awarenessCount = 0;
            for (int j = 0; j < _numOfBoids; j++)
            {
                if (i == j)
                    continue;
                neighborPos = _boidsList[j].transform.position;
                distance = Distance(boidPos, neighborPos);
                if (distance > _boidAwarenessRadius)
                    continue;
                else
                {
                    if (IsInFOV(_boidsList[i], boidPos, neighborPos))
                    {
                        alignment += _boidsList[j].Velocity;
                        cohesion += neighborPos;
                        awarenessCount++;
                    }
                    if (distance < _boidMinDistance)
                    {
                        separation += GetSeparationVector(boidPos, neighborPos, distance);
                        minDistCount++;
                    }
                }
            }
            if (minDistCount > 1)
            {
                separation /= minDistCount;
            }
            if (awarenessCount > 0)
            {
                alignment /= awarenessCount;
                cohesion /= awarenessCount;
                cohesion -= boidPos;
            }
            _boidsList[i].AddForce(separation, 0.65f);
            _boidsList[i].AddForce(alignment, 0.15f);
            _boidsList[i].AddForce(cohesion, 0.05f);
            _boidsList[i].AddForce(Bounding(boidPos), 0.1f);
        }
    }

    private void MoveBoids()
    {
        foreach (Boid boid in _boidsList)
        {
            boid.UpdateBoid(_boidMaxSpeed);
        }
    }

    private Vector3 GetSeparationVector(Vector3 boid, Vector3 target, float distance)
    {
        Vector3 diff = boid - target;
        float ratio = Mathf.Clamp01(1.0f - distance / _boidMinDistance);
        return diff.normalized * (ratio / distance);
    }

    private bool IsInFOV(Boid boid, Vector3 boidPos, Vector3 neighborPos)
    {
        Vector3 dir = neighborPos - boidPos;
        float dot = Vector3.Dot(boid.Velocity, dir);
        if (dot > MathF.Cos(_FoVAngle * Mathf.Deg2Rad))
        {
            return true;
        }
        return false;
    }

    private Vector3 Bounding(Vector3 boid)
    {
        Vector3 v = Vector3.zero;
        float bx, by;
        bx = boid.x;
        by = boid.y;
        if (bx > _boundingBox.xMax)
        {
            v.x = _boundingBox.xMax - bx;
        }
        else
        if (bx < _boundingBox.xMin)
        {
            v.x = _boundingBox.xMin - bx;
        }
        if (by > _boundingBox.yMax)
        {
            v.y = _boundingBox.yMax - by;
        }
        else
        if (by < _boundingBox.yMin)
        {
            v.y = _boundingBox.yMin - by;
        }
        v *= 8f;
        return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float Distance(Vector3 a, Vector3 b)
    {
        float num = a.x - b.x;
        float num2 = a.y - b.y;
        return (float)Math.Sqrt(num * num + num2 * num2);
    }
}

[BurstCompile]
public struct ControlBoidsJob : IJobParallelForTransform
{
    public void Execute(int index, TransformAccess transform)
    {
        throw new NotImplementedException();
    }
}