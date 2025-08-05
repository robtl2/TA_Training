using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.Diagnostics;

/// <summary>
/// 呐，几乎干的所有事情就是写下来我给的菜品方案而已
/// 然后每样菜写之前和写之后还顺便告诉下别人，这时别人还能再加点料到菜单上
/// </summary>
namespace PixRenderPipline
{
    public class PixDeferredRenderer : PixRenderer
    {
        public EarlyZPass earlyZPass { get; private set; }
        public OcclusionCullingPass occlusionCullingPass { get; private set; }
        public GBufferPass gBufferPass { get; private set; }
        public TiledPass tiledPass { get; private set; }
        public DeferredPass deferredPass { get; private set; }
        public SkyPass skyPass { get; private set; }
        public DecalPass decalPass { get; private set; }
        public TransparentPass transparentPass { get; private set; }
        public PostProcessPass postProcessPass { get; private set; }
        public FinalPass finalPass { get; private set; }

        /// <summary>
        /// 上一帧后处理之前的渲染结果
        /// </summary>
        public Dictionary<Camera, RenderTexture> frontRT = new();

        public PixDeferredRenderer()
        {
            earlyZPass = new(this);
            occlusionCullingPass = new(this);
            gBufferPass = new(this);
            tiledPass = new(this);
            deferredPass = new(this);
            skyPass = new(this);
            decalPass = new(this);
            transparentPass = new(this);
            postProcessPass = new(this);
            finalPass = new(this);
        }

        public override void Render()
        {
            base.Render();

            if (asset.enable_TAA)
            {
                if (!frontRT.ContainsKey(camera) || frontRT[camera].width != size.x || frontRT[camera].height != size.y)
                {
                    if (frontRT.ContainsKey(camera))
                        frontRT[camera].Release();

                    frontRT[camera] = new RenderTexture(size.x, size.y, 0, RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
                    frontRT[camera].name = "_ColorTex_Front";
                    frontRT[camera].Create();
                }
            }
            

            ExecutePass(earlyZPass);
            ExecutePass(occlusionCullingPass);
            ExecutePass(gBufferPass);
            ExecutePass(tiledPass);
            ExecutePass(deferredPass);
            ExecutePass(skyPass);
            ExecutePass(decalPass);
            ExecutePass(transparentPass);
            ExecutePass(postProcessPass);
            ExecutePass(finalPass);

#if UNITY_EDITOR
            // 绘制编辑器视图中的Gizmos
            if (isSceneView)
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
#endif

            //菜单以及工艺都写完了，交还给厨房管事儿的
            context.Submit();
        }

        void ExecutePass(PixPassBase pass)
        {
            pass.Execute();
        }

        public override void CleanUp()
        {
            base.CleanUp();

            foreach (var rt in frontRT)
                rt.Value.Release();

            frontRT.Clear();
        }
    }
}