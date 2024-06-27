#1. Simple boids using game objects, no parallelization, 200 boids:\
<img src="https://github.com/WojciechPGH/ECS-Boids/assets/52805836/565eb7f4-9b5e-4f09-b393-9c312eeb72f8" width="45%">
<img src="https://github.com/WojciechPGH/ECS-Boids/assets/52805836/a58963e3-794b-472a-b4a9-aff3d61b8c92" width="45%">

Code:
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
