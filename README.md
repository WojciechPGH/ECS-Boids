### 1. Simple boids using game objects, no parallelization, 200 boids:
<img src="https://github.com/WojciechPGH/ECS-Boids/assets/52805836/565eb7f4-9b5e-4f09-b393-9c312eeb72f8" width="45%">
<img src="https://github.com/WojciechPGH/ECS-Boids/assets/52805836/a58963e3-794b-472a-b4a9-aff3d61b8c92" width="45%">

<details>
<summary>Code</summary>
    
```C#
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
```
</details>

### 2. Changing unity Vectors to unity mathematics floats3, changing boids position and rotation directly from boidsController, no parallelization, 200 boids:

<details>
    <summary>Code:</summary>

```C#
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
```
</details>

<img src="https://github.com/WojciechPGH/ECS-Boids/assets/52805836/6ad82da7-3637-491b-94f1-04937dcca1f8" width="45%">
<img src="https://github.com/WojciechPGH/ECS-Boids/assets/52805836/be43a669-d005-42c6-8bc7-e2e7489556c5" width="45%">
<br>Thanks to this approach I eliminated massive 80000! Transform.get_position calls, Component.get_transform with 40000 calls and 80000 calls from casting Vector3 to Vector2.

### 3. Parallelization:
<br>Changed lists to NativeArrays, created two jobs, one to handle boids positions and rotations, second for updating game objects, added burst compiler
<details>
<summary>Boids logic job:</summary>
    
```C#
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
    ...
    }
```
</details>
<details>
<summary>Boids copy transform job:</summary>
    
```C#
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
        
```
</details>
<details>
<summary>Boids controller update:</summary>
    
```C#
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

```
</details>
Outcome: from 117 avg fps to 159 with 200 boids
<br><img src="https://github.com/WojciechPGH/ECS-Boids/assets/52805836/06d044a8-037a-43a5-8ff9-f36614ddf066" width="45%">
<br>2000 boids
<br><img src="https://github.com/WojciechPGH/ECS-Boids/assets/52805836/f5dffd96-795c-4120-92f6-ce64bff72c4f" width="45%">

### 4. Getting rid of game objects:
Rendering is now done via Graphics.RenderMeshInstanced, no need for transform job
<details>
<summary>Boids renderer code:</summary>
    
```C#
    public class BoidsRendering
    {
        private readonly Material _material;
        private readonly Mesh _mesh;
        private readonly int _numInstances;
        private NativeArray<Matrix4x4> _transforms;
        public BoidsRendering(Material material, int numInstances)
        {
            _mesh = CreateMesh(0.15f, 0.3f);
            _material = material;
            _numInstances = numInstances;
            _transforms = new NativeArray<Matrix4x4>(numInstances, Allocator.Persistent);
        }
    
        public void DrawBoids(NativeArray<float3> positions, NativeArray<float3> velocity)
        {
            RenderParams rp = new(_material)
            {
                worldBounds = new Bounds(Vector3.zero, 100f * Vector3.one), // use tighter bounds for better FOV culling
                matProps = new MaterialPropertyBlock()
            };
    
    
            for (int i = 0; i < _numInstances; ++i)
            {
                Quaternion rot = GetDirection(velocity[i]);
                _transforms[i] = Matrix4x4.TRS(positions[i], rot, Vector3.one);
            }
            Graphics.RenderMeshInstanced(rp, _mesh, 0, _transforms);
        }
        ...
    }
```
</details>
2000 boids - got fps increase from 76 avg to 121 avg fps:
<br><img src="https://github.com/WojciechPGH/ECS-Boids/assets/52805836/93e78b8d-a738-4375-af14-eb6e179586ef" width="60%">

### 5. TODO:
    -Quadtree
    -DBSCAN 
    -Compute shader
