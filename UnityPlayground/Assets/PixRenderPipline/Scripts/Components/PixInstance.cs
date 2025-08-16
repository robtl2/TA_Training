using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PixRenderPipline
{
    [ExecuteInEditMode]
    public class PixInstance : MonoBehaviour
    {
        public static PixInstance instMan;
        public static List<PixInstance> list = new();
        public static Dictionary<GameObject, List<PixInstance>> instDict = new();

        static Dictionary<GameObject, Bounds> boundsDict = new();
        static Dictionary<GameObject, Matrix4x4[]> localMatricesDict = new();
        static Dictionary<GameObject, Mesh[]> meshesDict = new();
        static Dictionary<GameObject, Material[][]> materialsDict = new();
        static Dictionary<GameObject, bool> dirtyDict = new();
        static Dictionary<GameObject, Matrix4x4> targetPreviousMatrixDic = new();


        public GameObject target;

        public Bounds bounds { get; private set; }

        GameObject _prevTarget;

        Matrix4x4 _prevLocalToWorld = Matrix4x4.identity;

        public static void UpdatePrevioursMatrix()
        {
            foreach (var inst in list)
                inst._prevLocalToWorld = inst.transform.localToWorldMatrix;

            var targets = targetPreviousMatrixDic.Keys.ToArray();
            foreach (var t in targets)
                targetPreviousMatrixDic[t] = t.transform.localToWorldMatrix;
        }

        /// <summary>
        /// 给运行时用的
        /// </summary>
        /// <param name="target"></param>
        public void SetTarget(GameObject target)
        {
            this.target = target;
            MakeDirty();
        }

        void OnEnable()
        {
            if (instMan == null)
                instMan = this;

            list.Add(this);
            MakeDirty();
        }

        void OnDisable()
        {
            list.Remove(this);
            MakeDirty();

            if (this == instMan)
            {
                instMan = null;

                if (list.Count > 0)
                    instMan = list[0];
                else
                {
                    Clear();
                }
            }
        }

        void OnDestroy()
        {
            Clear();   
        }

        void Clear()
        { 
            instDict.Clear();
            dirtyDict.Clear();
        }

        void Update()
        {
            UpdateBounds();
        }

        void UpdateBounds()
        {
            if (!target) return;
            if (!boundsDict.ContainsKey(target) || boundsDict[target] == null) return;

            var localBounds = boundsDict[target];
            bounds = PixUtils.CalculateWorldBounds(transform, localBounds);
            // bounds.center = bounds.center + transform.position;
            // bounds = new Bounds(localBounds.center + transform.position - target.transform.position, localBounds.size);
        }

        void OnValidate()
        {
            if (target != _prevTarget)
            {
                MakeDirty();
                _prevTarget = target;
            }
        }

        void OnDrawGizmosSelected()
        {
            if (!PixRenderSetting.instance || !PixRenderSetting.instance.showInstancingBounds)
                return;

            Vector3[] corners = PixUtils.GetBoundsCorners(bounds);

            Gizmos.color = Color.green;
            // 使用 Gizmos.DrawLine 绘制线框
            // 后面四个点
            Gizmos.DrawLine(corners[0], corners[1]); // 左下后 -> 右下后
            Gizmos.DrawLine(corners[1], corners[3]); // 右下后 -> 右上后
            Gizmos.DrawLine(corners[3], corners[2]); // 右上后 -> 左上后
            Gizmos.DrawLine(corners[2], corners[0]); // 左上后 -> 左下后
            // 前面四个点
            Gizmos.DrawLine(corners[4], corners[5]); // 左下前 -> 右下前
            Gizmos.DrawLine(corners[5], corners[7]); // 右下前 -> 右上前
            Gizmos.DrawLine(corners[7], corners[6]); // 右上前 -> 左上前
            Gizmos.DrawLine(corners[6], corners[4]); // 左上前 -> 左下前
            // 连接前面和后面的边
            Gizmos.DrawLine(corners[0], corners[4]); // 左下后 -> 左下前
            Gizmos.DrawLine(corners[1], corners[5]); // 右下后 -> 右下前
            Gizmos.DrawLine(corners[2], corners[6]); // 左上后 -> 左上前
            Gizmos.DrawLine(corners[3], corners[7]); // 右上后 -> 右上前
        }

        void MakeDirty()
        {
            if (_prevTarget) dirtyDict[_prevTarget] = true;
            if (target) dirtyDict[target] = true;
        }

        void LateUpdate()
        {
            if (this != instMan) return;

            mpb ??= new();

            var objs = dirtyDict.Keys.ToArray();

            foreach (var obj in objs)
            {
                if (dirtyDict[obj])
                    RefreshList(obj);
            }
        }

        static Matrix4x4[] worldMatrices = new Matrix4x4[1023];
        static Matrix4x4[] preWorldMatrices = new Matrix4x4[1023];
        static MaterialPropertyBlock mpb;
        public static void DrawPass(PixRenderer renderer, int passID)
        {
            if (list.Count < 1) return;

            foreach (var kv in instDict) // per target loop
            {
                GameObject tar = kv.Key;
                List<PixInstance> insts = kv.Value;

                if (!meshesDict.TryGetValue(tar, out Mesh[] meshes)) continue;
                if (meshes.Length == 0) continue;

                worldMatrices[0] = tar.transform.localToWorldMatrix;
                preWorldMatrices[0] = targetPreviousMatrixDic[tar];

                for (int i = 0; i < meshes.Length; i++) // one mesh one drawcall
                {
                    mpb.Clear();

                    var mesh = meshes[i];
                    var localMatrix = localMatricesDict[tar][i];
                    var mats = materialsDict[tar][i];

                    for (int j = 0; j < mats.Length; j++)
                    { 
                        int count = 1;
                        foreach (var inst in insts) // per instance loop
                        {
                            if (renderer.FrustumCull(inst.bounds))
                            {
                                worldMatrices[count] = inst.transform.localToWorldMatrix * localMatrix;
                                preWorldMatrices[count] = inst._prevLocalToWorld;
                                count++;
                            }
                        }

                        mpb.SetMatrixArray("_PreviousLocalToWorld", preWorldMatrices);

                        renderer.cmb.DrawMeshInstanced(mesh, j, mats[j], passID, worldMatrices, count, mpb);
                    }
                    
                }
            }
        }

        void RefreshList(GameObject obj)
        {
            if (obj == null) return;

            if (!instDict.ContainsKey(obj))
                instDict[obj] = new();

            RefreshRefrence(obj);
            targetPreviousMatrixDic[obj] = obj.transform.localToWorldMatrix;

            instDict[obj].Clear();

            foreach (var inst in list)
            {
                if (instDict[obj].Count >= 1023) break;

                if (inst.target == obj)
                    instDict[obj].Add(inst);
            }

            dirtyDict[obj] = false;
        }

        void RefreshRefrence(GameObject obj)
        {
            if (obj == null) return;

            Renderer[] rens = obj.GetComponentsInChildren<Renderer>();

            int count = rens.Length;
            Mesh[] meshes = new Mesh[count];
            Material[][] materials = new Material[count][];
            Matrix4x4[] matrices = new Matrix4x4[count];
            Bounds[] boundses = new Bounds[count];
            Matrix4x4 worldToLocal = obj.transform.worldToLocalMatrix;

            for (int i = 0; i < count; i++)
            {
                var ren = rens[i];

                Vector3[] corners = PixUtils.GetBoundsCorners(ren.bounds);

                for (int j = 0; j < 8; j++)
                {
                    corners[j] = obj.transform.worldToLocalMatrix.MultiplyPoint3x4(corners[j]);
                }

                boundses[i] = new Bounds(corners[0],Vector3.zero);
                for (int j = 1; j < 8; j++)
                {
                    boundses[i].Encapsulate(corners[j]);
                }

                if (ren is MeshRenderer)
                {
                    if (ren.TryGetComponent(out MeshFilter mf))
                        meshes[i] = mf.sharedMesh;
                }
                else if (ren is SkinnedMeshRenderer)
                {
                    var skinRen = ren as SkinnedMeshRenderer;
                    meshes[i] = skinRen.sharedMesh;
                }

                Material[] mats = ren.sharedMaterials;
                foreach(var mat in mats)
                    if(mat)mat.enableInstancing = true;
                        
                materials[i] = mats;

                Matrix4x4 matrixWorld = ren.transform.localToWorldMatrix;
                Matrix4x4 matrixLocal = worldToLocal * matrixWorld;
                matrices[i] = matrixLocal;
            }

            boundsDict[obj] = MergeBounds(boundses);
            meshesDict[obj] = meshes;
            localMatricesDict[obj] = matrices;
            materialsDict[obj] = materials;
        }

        Bounds MergeBounds(Bounds[] boundsArray)
        {
            if (boundsArray == null || boundsArray.Length == 0)
                return new Bounds(Vector3.zero, Vector3.zero);

            Bounds mergedBounds = boundsArray[0];
            for (int i = 1; i < boundsArray.Length; i++)
            {
                mergedBounds.Encapsulate(boundsArray[i]);
            }

            return mergedBounds;
        }
    }
}
