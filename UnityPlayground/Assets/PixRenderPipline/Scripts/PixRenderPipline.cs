using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PixRenderPipline
{
    /// <summary>
    /// 渲染管线主入口
    /// 就干一个事儿：调度Renderer遍历渲染相机
    /// </summary>
    public class PixRenderPipline : RenderPipeline
    {
        public PixRenderPiplineAsset asset { get; private set; }
        public PixRenderer deferredRenderer { get; private set; }
        public PixRenderer forwardRenderer{ get; private set; }

        int frameIndex = 0;
        public PixRenderPipline(PixRenderPiplineAsset asset)
        {
            this.asset = asset;
            deferredRenderer = new PixDeferredRenderer();
            forwardRenderer = new PixForwardRenderer();
        }

        /// <summary>
        /// 渲染管线主入口
        /// </summary>
        /// <param name="context">厨房管事儿的给过来的空菜单</param>
        /// <param name="cameras">点菜的客人</param>
        [Obsolete("下个版本的API就把Array换成了List，闲出屁了")]
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            if (!PixRenderSetting.instance) return;

            var setting = PixRenderSetting.instance;
            var renderer = setting.pipeline == PixRenderSetting.Pipeline.Forward ? forwardRenderer : deferredRenderer;
            
            frameIndex++;
            // 按照深度排序相机
            System.Array.Sort(cameras, (c1, c2) => c1.depth.CompareTo(c2.depth));
            // cameras.Sort((c1, c2) => c1.depth.CompareTo(c2.depth));

            foreach (var camera in cameras)
            {
                renderer.Setup(context, camera, asset, frameIndex);
                renderer.Render();
            }

            PixInstance.UpdatePrevioursMatrix();

            renderer.jitterUploaded = false;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            deferredRenderer.CleanUp();
            forwardRenderer.CleanUp();
        }
    }
}
