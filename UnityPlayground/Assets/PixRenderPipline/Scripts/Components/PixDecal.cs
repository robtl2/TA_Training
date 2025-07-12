using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class PixDecal : MonoBehaviour
{
    public static List<PixDecal> decals = new();

    public enum DecalShadingModel
    {
        Unlit,
        Lit,
    }

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    Material material;

    public DecalShadingModel shadingModel;

    public Texture2D texture;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        meshFilter.hideFlags = HideFlags.HideInInspector;
        meshRenderer.hideFlags = HideFlags.HideInInspector;
    }

    void OnEnable()
    { 
        decals.Add(this);
    }

    void OnDisable()
    { 
        decals.Remove(this);
    }

    void Update()
    {

    }
}
