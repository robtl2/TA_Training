using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

[ExecuteInEditMode]
public class PathDemorm : MonoBehaviour
{
    public SplineContainer targetPath;
    [Range(0, 0.999f)]
    public float Progress;

    public Vector2 Scale;

    public bool debug;

    List<Material> mats = new List<Material>();
    Vector4[] _PathKnots;
    Vector4[] _PathNormals;
    Vector4[] _PathTangentsIn;
    Vector4[] _PathTangentsOut;
    int _PathKnotCount;
    Vector4 _Pivot;
    float _PathLength;

    void OnEnable()
    {
        Init();
        Refresh();
    }

    void OnDisable()
    {
        foreach (var mat in mats)
            mat.DisableKeyword("_PIVOT_ON");
    }

    void Update()
    {
        Refresh();
    }

    void Init()
    {
        InitSplineData();
        InitMaterials();
    }

    void InitSplineData()
    {
        _PathKnots = new Vector4[64];
        _PathNormals = new Vector4[64];
        _PathTangentsIn = new Vector4[64];
        _PathTangentsOut = new Vector4[64];
        _PathKnotCount = 2;
    }

    void InitMaterials()
    { 
        mats = GetComponentsInChildren<MeshRenderer>().SelectMany(r => r.sharedMaterials)
            .Concat(GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(r => r.sharedMaterials))
            .Where(m => m != null)
            .Distinct().ToList();
    }

    void Refresh()
    {
        _PathLength = targetPath.Splines[0].GetLength();
        _Pivot = new Vector4(transform.position.x, transform.position.y, transform.position.z, 1);

        var knots = targetPath.Splines[0].Knots.ToArray();
        for (int i = 0; i < knots.Length; i++)
        {
            var knot = knots[i];
            var pos = knot.Position;
            var tangentIn = knot.TangentIn;
            var tangentOut = knot.TangentOut;
            var rotation = knot.Rotation;
            float3 normal = math.mul(math.normalize(rotation), new float3(-1, 0, 0));
            tangentIn = pos + math.mul(math.normalize(rotation), tangentIn);
            tangentOut = pos + math.mul(math.normalize(rotation), tangentOut);

            _PathKnots[i] = new Vector4(pos.x, pos.y, pos.z, 0);
            _PathTangentsIn[i] = new Vector4(tangentIn.x, tangentIn.y, tangentIn.z, 0);
            _PathTangentsOut[i] = new Vector4(tangentOut.x, tangentOut.y, tangentOut.z, 0);
            _PathNormals[i] = new Vector4(normal.x, normal.y, normal.z, 0);
        }
        _PathKnotCount = knots.Length;

        foreach (var mat in mats)
        {
            mat.SetVectorArray("_PathKnots", _PathKnots);
            mat.SetVectorArray("_PathNormals", _PathNormals);
            mat.SetVectorArray("_PathTangentsIn", _PathTangentsIn);
            mat.SetVectorArray("_PathTangentsOut", _PathTangentsOut);
            mat.SetInt("_PathKnotCount", _PathKnotCount);

            mat.SetVector("_Pivot", _Pivot);
            mat.SetFloat("_Progress", Progress);
            mat.SetFloat("_PathLength", _PathLength);
            mat.SetVector("_Scale", new Vector4(Scale.x, Scale.y, 0, 0));

            mat.EnableKeyword("_PIVOT_ON");
        }
    }

    void OnDrawGizmos()
    {
        if (!debug) return;

        for (int i = 0; i < _PathKnotCount; i++)
        {
            var knot = transform.TransformPoint(_PathKnots[i]);
            var normal = knot + transform.TransformPoint(_PathNormals[i])*0.5f;
            var tangentIn = transform.TransformPoint(_PathTangentsIn[i]);
            var tangentOut = transform.TransformPoint(_PathTangentsOut[i]);

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(new Vector3(knot.x, knot.y, knot.z), 0.1f);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(new Vector3(tangentIn.x, tangentIn.y, tangentIn.z), 0.06f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(new Vector3(tangentOut.x, tangentOut.y, tangentOut.z), 0.06f);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(new Vector3(normal.x, normal.y, normal.z), 0.06f);
        }
    }

}
