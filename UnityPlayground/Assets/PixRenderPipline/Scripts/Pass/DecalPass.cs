using System.Collections.Generic;
using UnityEngine;

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
    List<PixDecal> visibleDecals = new();
    
    int _MainTex = Shader.PropertyToID("_MainTex");
    int _MainTex_ST = Shader.PropertyToID("_MainTex_ST");
    int _ShadingModel = Shader.PropertyToID("_ShadingModel");
    
    /// 没有使用SRP batch, 因为我感觉SRP batch的性能不如直接DrawMeshInstanced
    /// 如果我感觉错了, 那就亏大了
    /// 这样就用不了renderer在开头做的CullingResults, 
    /// 这里用的GeometryUtility.CalculateFrustumPlanes 和 GeometryUtility.TestPlanesAABB
    /// 应该比较高效....吧..., 毕竟是引擎提供的高效API
    /// 注意：严肃场合遇到多个方案时都应该留着，做了足够的性能测试才能取舍
    public override void Execute()
    {
        base.Execute();
        
        visibleDecals.Clear();

        // 手动进行视锥体剔除，使用Unity的GeometryUtility
        var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(renderer.camera);
        
        // 收集可见的Decal
        for (int i = 0; i < PixDecal.decals.Count; i++)
        {
            var decal = PixDecal.decals[i];
            if (GeometryUtility.TestPlanesAABB(frustumPlanes, decal.WorldBounds))
                visibleDecals.Add(decal);
        }

        if (visibleDecals.Count == 0)
            return;

        renderer.cmb.SetRenderTarget(DeferredPass.ColorBuff, EarlyZPass.depthID);

        // 先把前两个stencil pass全画了，因为这两个pass没有参数
        for (int i = 0; i < visibleDecals.Count; i++)
            decalsMatrix[i] = visibleDecals[i].transform.localToWorldMatrix;
        renderer.cmb.DrawMeshInstanced(PixDecal.mesh, 0, material, 0, decalsMatrix, visibleDecals.Count);
        renderer.cmb.DrawMeshInstanced(PixDecal.mesh, 0, material, 1, decalsMatrix, visibleDecals.Count);

        // 然后按atlas来画mainPass
        foreach (var atlas in PixDecal.decalsBySpriteAtlas.Keys)
        {
            decalsPropertyBlock.Clear();
            rects.Clear();
            shadingModels.Clear();

            Texture2D texture = Texture2D.whiteTexture;
            var decals = PixDecal.decalsBySpriteAtlas[atlas];

            // 只处理可见的Decal
            var visibleDecalsInAtlas = new List<PixDecal>();
            foreach (var decal in decals)
            {
                if (visibleDecals.Contains(decal))
                    visibleDecalsInAtlas.Add(decal);
            }
            
            if (visibleDecalsInAtlas.Count == 0)
                continue;

            // 按order排序
            visibleDecalsInAtlas.Sort((a, b) => a.order.CompareTo(b.order));
            int decalsCount = visibleDecalsInAtlas.Count;

            // 设置material参数
            for (int i = 0; i < decalsCount; i++)
            {
                var decal = visibleDecalsInAtlas[i];
                texture = decal.Texture;
                decalsMatrix[i] = decal.transform.localToWorldMatrix;

                rects.Add(decal.UV_ST);
                shadingModels.Add((int)decal.shadingModel);
            }
            material.SetTexture(_MainTex, texture);
            decalsPropertyBlock.SetVectorArray(_MainTex_ST, rects.ToArray());
            decalsPropertyBlock.SetFloatArray(_ShadingModel, shadingModels.ToArray());

            renderer.cmb.DrawMeshInstanced(PixDecal.mesh, 0, material, 2, decalsMatrix, decalsCount, decalsPropertyBlock);
        }

        renderer.context.ExecuteCommandBuffer(renderer.cmb);
        renderer.cmb.Clear();
    }
}