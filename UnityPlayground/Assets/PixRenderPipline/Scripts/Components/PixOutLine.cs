using System.Collections.Generic;
using UnityEngine;

namespace PixRenderPipline
{   
    [ExecuteInEditMode]
    public class PixOutLine : MonoBehaviour
    {
        public Color color = Color.black;
        public float width = 0.05f;
        public float zOffset = 0;

        HashSet<Renderer> renderers = new();
        Dictionary<Material, int> passIDs = new();

        static readonly int _OutlineColor = Shader.PropertyToID("_OutlineColor");
        static readonly int _OutlineWidth = Shader.PropertyToID("_OutlineWidth");
        static readonly int _OutlineZOffset = Shader.PropertyToID("_OutlineZOffset");

        void OnEnable()
        {
            RefreshRenderers();
            PixRenderEvent.AddEvent(PixRenderEventName.BeforeTransparent, WhenBeforeTransparent);
        }
        void OnDisable()
        {
            PixRenderEvent.RemoveEvent(PixRenderEventName.BeforeTransparent, WhenBeforeTransparent);
        }

        void WhenBeforeTransparent(PixRenderer renderer)
        {
            foreach (var r in renderers)
            {
                var mat = r.sharedMaterial;

                if (renderer.FrustumCull(r.bounds))
                    renderer.cmb.DrawRenderer(r, mat, 0, passIDs[mat]);
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
                    int passID = mat.FindPass("PixBackHull");
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