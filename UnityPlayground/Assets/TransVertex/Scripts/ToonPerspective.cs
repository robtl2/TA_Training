using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[ExecuteInEditMode]
public class ToonPerspective : MonoBehaviour
{
    public Vector3 Offset;
    Vector3 _Pivot;
    List<Material> mats;

    void OnEnable()
    {
        InitMaterials();
        Refresh();
    }

    void OnDisable()
    {
        foreach (var mat in mats)
            mat.DisableKeyword("_PIVOT_ON");
    }

    void Update()
    {
        if (transform.hasChanged)
        {
            Refresh();
            transform.hasChanged = false;
        }
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
        _Pivot = transform.position + Offset; 

        foreach (var mat in mats)
        {
            if (mat && mat.HasProperty("_Pivot"))
            { 
                mat.EnableKeyword("_PIVOT_ON");
                mat.SetVector("_Pivot", new Vector4(_Pivot.x, _Pivot.y, _Pivot.z, 1));
            }
        }
    }
}
