using UnityEngine;
using System.Collections.Generic;
using UnityEngine.U2D;

[ExecuteInEditMode]
public class PixDecal : MonoBehaviour
{
    public static List<PixDecal> decals = new();
    public static Dictionary<SpriteAtlas, List<PixDecal>> decalsBySpriteAtlas = new();

    static Mesh _mesh;
    public static Mesh mesh
    {
        get
        {
            if (_mesh == null)
            {
                var tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _mesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
                DestroyImmediate(tempCube);
            }
            return _mesh;
        }
    }

    public enum DecalShadingModel
    {
        Unlit,
        Lit,
    }

    public DecalShadingModel shadingModel;
    public SpriteAtlas spriteAtlas;
    public string spriteName;

    public uint order = 0;

    void OnEnable()
    {
        if (spriteAtlas == null) return;

        decals.Add(this);

        if (!decalsBySpriteAtlas.ContainsKey(spriteAtlas))
            decalsBySpriteAtlas[spriteAtlas] = new List<PixDecal>();

        decalsBySpriteAtlas[spriteAtlas].Add(this);
    }

    void OnDisable()
    {
        if (spriteAtlas == null) return;

        decals.Remove(this);

        if (decalsBySpriteAtlas.ContainsKey(spriteAtlas))
            decalsBySpriteAtlas[spriteAtlas].Remove(this);
    }


    

    
}
