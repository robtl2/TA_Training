using System.Collections.Generic;
using UnityEngine;

namespace PixRenderPipline
{
    
    [ExecuteInEditMode]
    public class PixMesh : MonoBehaviour
    {
        public static List<MeshRenderer> allMeshRenderers = new();

        static PixMesh pixMeshMan;
        static bool listIsDirty = true;
        static List<PixMesh> pixMeshes = new();

        static int _PreviousLocalToWorld = Shader.PropertyToID("_PreviousLocalToWorld");
        static MaterialPropertyBlock _PreviousLocalToWorldBlock;
        static Dictionary<MeshRenderer, Matrix4x4> previousLocalToWorldMatrices = new();

        MeshRenderer[] renderers;


        void OnEnable()
        {
            if (_PreviousLocalToWorldBlock == null)
                _PreviousLocalToWorldBlock = new();

            if (pixMeshMan == null)
                pixMeshMan = this;

            pixMeshes.Add(this);
            listIsDirty = true;

            renderers = GetComponentsInChildren<MeshRenderer>();
        }

        void OnDisable()
        {
            pixMeshes.Remove(this);
            listIsDirty = true;

            if (pixMeshMan == this)
            {
                pixMeshMan = null;
                if (pixMeshes.Count > 0)
                    pixMeshMan = pixMeshes[0];
            }

            renderers = new MeshRenderer[0];
        }

        void LateUpdate()
        {
            if (this != pixMeshMan)
                return;

            _PreviousLocalToWorldBlock.Clear();
            foreach (var renderer in allMeshRenderers)
            {
                if (renderer == null) continue;

                if (!previousLocalToWorldMatrices.TryGetValue(renderer, out var previousMatrix))
                {
                    previousMatrix = renderer.localToWorldMatrix;
                    previousLocalToWorldMatrices[renderer] = previousMatrix;
                }

                _PreviousLocalToWorldBlock.SetMatrix(_PreviousLocalToWorld, previousMatrix);
                renderer.SetPropertyBlock(_PreviousLocalToWorldBlock);

                previousLocalToWorldMatrices[renderer] = renderer.localToWorldMatrix;
            }

            if (!listIsDirty) return;

            allMeshRenderers.Clear();
            foreach (var pixMesh in pixMeshes)
            {
                if (pixMesh == null)
                    continue;

                foreach (var renderer in pixMesh.renderers)
                {
                    if (renderer != null && !allMeshRenderers.Contains(renderer))
                        allMeshRenderers.Add(renderer);
                }
            }
            listIsDirty = false;
        }

    }
}
