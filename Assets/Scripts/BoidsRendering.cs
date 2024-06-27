using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;


//using Unity.Physics;
using UnityEngine;

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

    private Quaternion GetDirection(float3 velocity)
    {
        float angle = math.atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        return Quaternion.Euler(0f, 0f, angle - 90f);
    }

    private Mesh CreateMesh(float quadSize = 0.5f, float xScale = 1f, float yScale = 1f)
    {
        Mesh mesh = new()
        {
            vertices = new Vector3[]
            {
                new Vector3(-quadSize * xScale, -quadSize * yScale, 0f),
                new Vector3(quadSize * xScale, -quadSize * yScale, 0f),
                new Vector3(quadSize * xScale, quadSize * yScale, 0f),
                new Vector3(-quadSize * xScale, quadSize * yScale, 0f)
            },
            triangles = new int[]
            {
                0,3,2,0,2,1
            },
            uv = new Vector2[]
            {
                Vector2.zero,
                new Vector2(1f,0f),
                new Vector2(1f,1f),
                new Vector2(0f,1f),
            }
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}
