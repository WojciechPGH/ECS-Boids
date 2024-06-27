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
