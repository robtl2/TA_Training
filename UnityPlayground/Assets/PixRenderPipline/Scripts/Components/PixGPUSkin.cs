using System.Collections.Generic;
using UnityEngine;


namespace PixRenderPipline
{   
    /// <summary>
    /// 在VS阶段做Skin的变换，
    /// 如果遇到相同的Mesh和Material，那SkinnedMesh也可以用Instance一次画多个了
    /// 倒不是心血来潮想写个这个，只是因为准备做TAA，到了拿motionVector时干脆顺便就把这个做了
    /// </summary>
    [ExecuteInEditMode]
    public class PixGPUSkin : MonoBehaviour
    {
        public static List<PixGPUSkin> gpuSkins = new List<PixGPUSkin>();
        public static PixGPUSkin gpuSkinMan;
        public static bool listIsDirty = true;

        Dictionary<SkinnedMeshRenderer, Mesh> meshes = new();
        bool passAdded = false;
        void OnEnable()
        {
            if (gpuSkinMan == null)
                gpuSkinMan = this;

            gpuSkins.Add(this);
            listIsDirty = true;
        }

        void OnDisable()
        {
            gpuSkins.Remove(this);
            listIsDirty = true;

            foreach (var rm in meshes)
                rm.Key.enabled = true;

            if (passAdded)
            {
                PixRenderEvent.RemoveEvent(PixRenderEventName.AfterEarlyZ, ExecuteGPUskinPass);
                PixRenderEvent.RemoveEvent(PixRenderEventName.AfterGBuffer, ExecuteGPUskinPass);
                passAdded = false;
            }

            if (gpuSkinMan == this)
            {
                gpuSkinMan = null;
                if (gpuSkins.Count > 0)
                    gpuSkinMan = gpuSkins[0];
            }

            foreach (var m in meshes)
            {
                var ren = m.Key;
                var mat = ren.sharedMaterial;
                mat.DisableKeyword("GPU_SKIN");
            }
        }

        public void ExecuteGPUskinPass(PixRenderer renderer)
        {
            int passID = 0;
            if (renderer.cmb.name == "PixEarlyZPass")
                passID = 1;
            else if (renderer.cmb.name == "PixShadowMap")
                passID = 2;

            foreach (var rm in meshes)
            {
                var ren = rm.Key;
                var mat = ren.sharedMaterial;
                var mesh = rm.Value;

                if (renderer.FrustumCull(mesh.bounds))
                    renderer.cmb.DrawMesh(mesh, ren.transform.localToWorldMatrix, mat, 0, passID);
            }
        }

        void RefreshRenderers()
        {
            meshes.Clear();

            var renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var ren in renderers)
            {
                var mesh = ren.sharedMesh;
                meshes[ren] = mesh;
            }

            foreach (var rm in meshes)
                rm.Key.enabled = false;

            if (enabled && !passAdded && meshes.Count > 0)
            {
                PixRenderEvent.AddEvent(PixRenderEventName.AfterEarlyZ, ExecuteGPUskinPass);
                PixRenderEvent.AddEvent(PixRenderEventName.AfterGBuffer, ExecuteGPUskinPass);
                passAdded = true;
            }
        }

        static int _CurrentPoses = Shader.PropertyToID("_CurrentPoses");
        static int _PreviousPoses = Shader.PropertyToID("_PreviousPoses");

        Matrix4x4[] previousPoses = new Matrix4x4[0];
        void Update()
        {
            foreach (var m in meshes)
            {
                var ren = m.Key;
                var mesh = m.Value;
                Matrix4x4[] bindposes = mesh.bindposes;

                Transform[] bones = ren.bones;
                Matrix4x4[] currentPoses = new Matrix4x4[bones.Length];
                for (int i = 0; i < bones.Length; i++)
                {
                    Transform pose = bones[i];
                    Matrix4x4 m_world = pose.localToWorldMatrix;
                    currentPoses[i] = m_world * bindposes[i];
                }

                var mat = ren.sharedMaterial;
                mat.EnableKeyword("GPU_SKIN");
                mat.SetMatrixArray(_CurrentPoses, currentPoses);

                if (previousPoses.Length > 0)
                    mat.SetMatrixArray(_PreviousPoses, previousPoses);

                previousPoses = currentPoses;
            }
        }

        void LateUpdate()
        {
            if (gpuSkinMan != this) return;
            if (!listIsDirty) return;

            listIsDirty = false;

            for (int i = 0; i < gpuSkins.Count; i++)
                gpuSkins[i].RefreshRenderers();
        }
    }
}