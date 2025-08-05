using System.Collections.Generic;
using UnityEngine;

namespace PixRenderPipline
{   
    [ExecuteInEditMode]
    public class PixCustomPass : MonoBehaviour
    {
        public string pass;
        public PixRenderEventName insetEvent = PixRenderEventName.BeforeTransparent;
        public int repeatTimes = 1;

        HashSet<Renderer> renderers = new();
        Dictionary<Material, int> passIDs = new();

        readonly int _PassIndex = Shader.PropertyToID("_PassIndex");

        void OnEnable()
        {
            RefreshRenderers();
            PixRenderEvent.AddEvent(insetEvent, WhenBeforeTransparent);
        }
        void OnDisable()
        {
            PixRenderEvent.RemoveEvent(insetEvent, WhenBeforeTransparent);
        }

        void WhenBeforeTransparent(PixRenderer renderer)
        {
            if (renderer.asset.enable_TAA) return;
            
            foreach (var r in renderers)
            {
                if (renderer.FrustumCull(r.bounds))
                {
                    var mat = r.sharedMaterial;

                    // TODO: 我感觉这里可以用DrawMeshInstanced优化
                    // 需要做个shortFur测试
                    for (int i = 0; i < repeatTimes; i++)
                    {
                        mat.SetInt(_PassIndex, i);
                        renderer.cmb.DrawRenderer(r, mat, 0, passIDs[mat]);
                    }
                }
            }
        }

        public void RefreshRenderers()
        {
            this.renderers.Clear();
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                foreach (var mat in materials)
                {
                    int passID = mat.FindPass(pass);
                    if (mat != null && mat.shader != null && passID >= 0)
                    {
                        passIDs[mat] = passID;
                        this.renderers.Add(renderer);
                        break; // 只要有一个材质满足即可
                    }
                }
            }
        }
    }
}