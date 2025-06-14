using UnityEngine;
using System.Collections.Generic;

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
       mats.Clear();
        foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
        {
            Material[] _mats;
            if (Application.isPlaying)
                _mats = renderer.materials;
            else
                _mats = renderer.sharedMaterials;

            foreach (var mat in _mats)
            {
                if (mat != null && !mats.Contains(mat))
                    mats.Add(mat);
            }
        }

        foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            Material[] _mats;
            if (Application.isPlaying)
                _mats = renderer.materials;
            else
                _mats = renderer.sharedMaterials;

            foreach (var mat in _mats)
            {
                if (mat != null && !mats.Contains(mat))
                    mats.Add(mat);
            }
        } 
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
