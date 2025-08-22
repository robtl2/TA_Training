using UnityEngine;

namespace PixRenderPipline
{
    public class SkyPass : PixPassBase
    {
        public SkyPass(PixRenderer renderer) : base("PixSkyPass", renderer) { }

        public override void Execute()
        {
            if (PixSky.instance == null || PixSky.instance.skyType == PixSky.SkyType.None) return;

            base.Execute();

            var setting = PixRenderSetting.instance;
            // var depthID = setting.pipeline == PixRenderSetting.Pipeline.Deferred ? EarlyZPass.depthID : ForwardEarlyZPass.buffID;

            var material = PixSky.instance.material;
            renderer.cmb.SetRenderTarget(OpaqueRT, EarlyZPass.buffID);
            renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, material, 0, 0);
            renderer.context.ExecuteCommandBuffer(renderer.cmb);
            renderer.cmb.Clear();
        }
    }
}
