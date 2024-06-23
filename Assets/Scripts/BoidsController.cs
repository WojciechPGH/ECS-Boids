using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Unity.Burst;
using UnityEngine.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.UI;

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
    [SerializeField]
    private Text debugText;

    private List<Boid> _boidsList;
    private NativeArray<float3> _boidsVelocity;
    private NativeArray<float3> _boidsPosition;
    private TransformAccessArray _boidsTransformArray;

    private void Start()
    {
        SetBoundingBox();
        InitBoids();
        debugText.text = "Number of boids: " + _numOfBoids.ToString();
    }

    private void Update()
    {
        //UpdateBoids();
        NativeArray<float3> outPos = new NativeArray<float3>(_numOfBoids, Allocator.TempJob);
        NativeArray<float3> outVel = new NativeArray<float3>(_numOfBoids, Allocator.TempJob);

        ControlBoidsJob boidsJob = new()
        {
            boidsPosition = _boidsPosition,
            boidsVelocity = _boidsVelocity,
            newBoidsPosition = outPos,
            newBoidsVelocity = outVel,
            deltaTime = Time.deltaTime,
            awarenessRadius = _boidAwarenessRadius,
            fovAngle = _FoVAngle,
            maxSpeed = _boidMaxSpeed,
            minDistance = _boidMinDistance,
            numOfBoids = _numOfBoids,
            boundary = new float4(_boundingBox.min, _boundingBox.max)
        };

        JobHandle boidsHandle = boidsJob.Schedule(_numOfBoids, 4);
        boidsHandle.Complete();
        outPos.CopyTo(_boidsPosition);
        outVel.CopyTo(_boidsVelocity);
        MoveBoids();
        outPos.Dispose();
        outVel.Dispose();
    }

    private void OnDestroy()
    {
        _boidsPosition.Dispose();
        _boidsVelocity.Dispose();
        _boidsTransformArray.Dispose();
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
        Transform[] boidsTransform = new Transform[_numOfBoids];
        _boidsList = new List<Boid>(_numOfBoids);
        _boidsVelocity = new NativeArray<float3>(_numOfBoids, Allocator.Persistent);
        _boidsPosition = new NativeArray<float3>(_numOfBoids, Allocator.Persistent);

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
            boidsTransform[i] = boid.transform;
            _boidsList.Add(boid);
            boidsVelocity = new float3(dir.x, dir.y, 0f) * _boidMaxSpeed;

            _boidsVelocity[i] = (boidsVelocity);
            _boidsPosition[i] = (boidPos);
        }
        _boidsTransformArray = new TransformAccessArray(boidsTransform);
    }


    private void MoveBoids()
    {
        CopyPositionsJob copyPositions = new CopyPositionsJob()
        {
            positions = _boidsPosition,
            velocity = _boidsVelocity
        };

        JobHandle copyPosJobHandle = copyPositions.Schedule(_boidsTransformArray);
        copyPosJobHandle.Complete();
    }
}

[BurstCompile]
public struct ControlBoidsJob : IJobParallelFor
{
    [ReadOnly]
    public int numOfBoids;
    [ReadOnly]
    public float awarenessRadius;
    [ReadOnly]
    public float minDistance;
    [ReadOnly]
    public float maxSpeed;
    [ReadOnly]
    public float fovAngle;
    /// <summary>
    /// x=xmin,y=ymin,z=xmax,w=ymax
    /// </summary>
    [ReadOnly]
    public float4 boundary;
    [ReadOnly]
    public float deltaTime;
    [ReadOnly]
    public NativeArray<float3> boidsVelocity;
    [ReadOnly]
    public NativeArray<float3> boidsPosition;
    [WriteOnly]
    public NativeArray<float3> newBoidsVelocity;
    [WriteOnly]
    public NativeArray<float3> newBoidsPosition;


    public void Execute(int i)
    {
        float distance;
        float minDistCount, awarenessCount;
        float3 boidPos, neighborPos;

        boidPos = boidsPosition[i];
        float3 separation = float3.zero;
        float3 alignment = float3.zero;
        float3 cohesion = float3.zero;
        minDistCount = 0;
        awarenessCount = 0;
        for (int j = 0; j < numOfBoids; j++)
        {
            if (i == j)
                continue;
            neighborPos = boidsPosition[j];
            distance = math.distance(boidPos, neighborPos);
            if (distance > awarenessRadius)
                continue;
            else
            {
                if (IsInFOV(boidsVelocity[i], boidPos, neighborPos))
                {
                    alignment += boidsVelocity[j];
                    cohesion += neighborPos;
                    awarenessCount++;
                }
                if (distance < minDistance)
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
        float3 acceleration = float3.zero;
        acceleration += AddForce(separation, 0.65f);
        acceleration += AddForce(alignment, 0.15f);
        acceleration += AddForce(cohesion, 0.05f);
        acceleration += AddForce(Bounding(boidPos), 0.5f);
        float vMagnitude = Magnitude(boidsVelocity[i] + acceleration);
        if (vMagnitude > maxSpeed)
        {
            newBoidsVelocity[i] = (boidsVelocity[i] / vMagnitude) * maxSpeed;
        }
        else
        {
            newBoidsVelocity[i] = boidsVelocity[i] + acceleration;
        }
        newBoidsPosition[i] = boidsPosition[i] + (boidsVelocity[i] + acceleration) * deltaTime;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="boundary">x=xmin, y=ymin, z=xmax, w=ymax</param>
    /// <param name="boidPos"></param>
    /// <returns></returns>
    private readonly float3 Bounding(float3 boidPos)
    {
        float3 v = float3.zero;
        float bx, by;
        bx = boidPos.x;
        by = boidPos.y;
        if (bx > boundary.z)
        {
            v.x = boundary.z - bx;
        }
        else
        if (bx < boundary.x)
        {
            v.x = boundary.x - bx;
        }
        if (by > boundary.w)
        {
            v.y = boundary.w - by;
        }
        else
        if (by < boundary.y)
        {
            v.y = boundary.y - by;
        }
        v *= 8f;
        return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly float3 AddForce(float3 force, float forceRatio)
    {
        return force * forceRatio;
    }

    private readonly bool IsInFOV(float3 boidVelocity, float3 boidPos, float3 neighborPos)
    {
        float3 dir = neighborPos - boidPos;
        float dot = math.dot(boidVelocity, dir);
        if (dot > math.cos(fovAngle * Mathf.Deg2Rad))
        {
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly float Magnitude(float3 v)
    {
        return (float)Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
    }

    private readonly float3 GetSeparationVector(float3 boid, float3 target, float distance)
    {
        float3 diff = boid - target;
        float ratio = math.clamp(1f - distance / minDistance, 0f, 1f);
        return math.normalize(diff) * (ratio / distance);
    }
}

[BurstCompile]
public struct CopyPositionsJob : IJobParallelForTransform
{
    [ReadOnly]
    public NativeArray<float3> positions;
    [ReadOnly]
    public NativeArray<float3> velocity;

    public void Execute(int index, TransformAccess transform)
    {
        transform.position = positions[index];
        transform.rotation = GetDirection(velocity[index]);
    }

    private readonly quaternion GetDirection(float3 velocity)
    {
        float angle = math.atan2(velocity.y, velocity.x);
        return quaternion.Euler(0f, 0f, angle - math.PIHALF);
    }
}