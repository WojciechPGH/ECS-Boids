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
        float3 pos, boidPos, boidsVelocity;
        float2 dir;

        for (int i = 0; i < _numOfBoids; i++)
        {
            pos = new float3(UnityEngine.Random.value, UnityEngine.Random.value, 0f);
            boidPos = new float3(_boundingBox.xMin + _boundingBox.width * pos.x, _boundingBox.yMin + _boundingBox.height * pos.y, 0f);
            boid = Instantiate(_boidPrefab).GetComponent<Boid>();
            dir = UnityEngine.Random.insideUnitCircle.normalized;
            boid.Init(boidPos, dir);

            _boidsList.Add(boid);
            boidsVelocity = new float3(dir.x, dir.y, 0f) * _boidMaxSpeed;

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
        float3 boidPos, neighborPos;

        for (int i = 0; i < _numOfBoids; i++)
        {
            boidPos = _boidsPosition[i];
            float3 separation = float3.zero;
            float3 alignment = float3.zero;
            float3 cohesion = float3.zero;
            minDistCount = 0;
            awarenessCount = 0;
            for (int j = 0; j < _numOfBoids; j++)
            {
                if (i == j)
                    continue;
                neighborPos = _boidsPosition[j];
                distance = Distance(boidPos, neighborPos);
                if (distance > _boidAwarenessRadius)
                    continue;
                else
                {
                    if (IsInFOV(_boidsVelocity[i], boidPos, neighborPos))
                    {
                        alignment += _boidsVelocity[j];
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

            AddForce(separation, 0.65f, i);
            AddForce(alignment, 0.15f, i);
            AddForce(cohesion, 0.05f, i);
            AddForce(Bounding(boidPos), 0.5f, i);
            _boidsVelocity[i] += _boidsAcceleration[i];
            float vMagnitude = Magnitude(_boidsVelocity[i]);
            if (vMagnitude > _boidMaxSpeed)
            {
                _boidsVelocity[i] = (_boidsVelocity[i] / vMagnitude) * _boidMaxSpeed;
            }
            _boidsPosition[i] += _boidsVelocity[i] * Time.deltaTime;
            _boidsAcceleration[i] = float3.zero;
            _boidsList[i].UpdateBoid(_boidsPosition[i], _boidsVelocity[i]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float Magnitude(float3 v)
    {
        return (float)Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
    }

    private void MoveBoids()
    {
        for (int i = 0; i < _boidsList.Count; i++)
        {
            Boid boid = _boidsList[i];
            boid.UpdateBoid(_boidsPosition[i], _boidsVelocity[i]);
        }
    }

    private float3 GetSeparationVector(float3 boid, float3 target, float distance)
    {
        Vector3 diff = boid - target;
        //float ratio = Mathf.Clamp01(1.0f - distance / _boidMinDistance);
        float ratio = math.clamp(1f - distance / _boidMinDistance, 0f, 1f);
        return diff.normalized * (ratio / distance);
    }

    private bool IsInFOV(float3 boidVelocity, float3 boidPos, float3 neighborPos)
    {
        float3 dir = neighborPos - boidPos;
        //float dot = Vector3.Dot(boidVelocity, dir);
        float dot = math.dot(boidVelocity, dir);
        if (dot > math.cos(_FoVAngle * Mathf.Deg2Rad))
        {
            return true;
        }
        return false;
    }

    private float3 Bounding(float3 boidPos)
    {
        float3 v = float3.zero;
        float bx, by;
        bx = boidPos.x;
        by = boidPos.y;
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
    private float Distance(float3 a, float3 b)
    {
        float num = a.x - b.x;
        float num2 = a.y - b.y;
        return (float)Math.Sqrt(num * num + num2 * num2);
    }

    private void AddForce(float3 force, float forceRatio, int boidIndex)
    {
        _boidsAcceleration[boidIndex] += force * forceRatio;
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