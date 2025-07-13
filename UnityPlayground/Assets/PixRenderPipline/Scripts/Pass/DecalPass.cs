using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.U2D;
using UnityEngine;
using Unity.Mathematics;

public class DecalPass : PixPassBase
{
    public DecalPass(PixRenderer renderer) : base("DecalPass", renderer)
    {
        material = new Material(Shader.Find("Pix/Decal"));
        material.enableInstancing = true;
    }

    Matrix4x4[] decalsMatrix = new Matrix4x4[1023];
    MaterialPropertyBlock decalsPropertyBlock = new();

    Material material;
    List<Vector4> rects = new();
    List<float> shadingModels = new();

    public override void Execute()
    {
        base.Execute();

        renderer.cmb.SetRenderTarget(DeferredPass.ColorBuff, EarlyZPass.depthID);
        int decalsCount = PixDecal.decals.Count;
        
        // 先把前两个stencil pass全画了，因为这两个pass没有参数
        for (int i = 0; i < PixDecal.decals.Count; i++)
            decalsMatrix[i] = PixDecal.decals[i].transform.localToWorldMatrix;
        renderer.cmb.DrawMeshInstanced(PixDecal.mesh, 0, material, 0, decalsMatrix, decalsCount);
        renderer.cmb.DrawMeshInstanced(PixDecal.mesh, 0, material, 1, decalsMatrix, decalsCount);

        // 然后按atlas来画mainPass
        foreach (var atlas in PixDecal.decalsBySpriteAtlas.Keys)
        {
            decalsPropertyBlock.Clear();
            rects.Clear();
            shadingModels.Clear();

            Texture2D texture = null;
            var decals = PixDecal.decalsBySpriteAtlas[atlas];
            decals.Sort((a, b) => a.order.CompareTo(b.order));
            decalsCount = decals.Count;

            for (int i = 0; i < decalsCount; i++)
            {
                var decal = decals[i];
                decalsMatrix[i] = decal.transform.localToWorldMatrix;

                float4 uv_st = new();
                int shadingModel = 0;
                var sprite = atlas.GetSprite(decal.spriteName);

                if (sprite != null)
                { 
                    texture = sprite.texture;
                    var rect = sprite.textureRect;
                    uv_st = new(rect.width, rect.height, rect.x, rect.y);
                    float2 size = new(texture.width, texture.height);
                    uv_st /= new float4(size, size);
                    shadingModel = (int)decal.shadingModel;
                }

                rects.Add(uv_st);
                shadingModels.Add(shadingModel);
            }

            material.SetTexture("_MainTex", texture);
            decalsPropertyBlock.SetVectorArray("_MainTex_ST", rects.ToArray());
            decalsPropertyBlock.SetFloatArray("_ShadingModel", shadingModels.ToArray());

            renderer.cmb.DrawMeshInstanced(PixDecal.mesh, 0, material, 2, decalsMatrix, decalsCount, decalsPropertyBlock);
        }

        renderer.context.ExecuteCommandBuffer(renderer.cmb);
        renderer.cmb.Clear();
    }
    
}