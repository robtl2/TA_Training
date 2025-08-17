using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PixRenderPipline
{
    [ExecuteInEditMode]
    public class PixInstance : MonoBehaviour
    {
        #region static
        //内部管理员
        static PixInstance manager;
        //场景中所有挂载了PixInstance的List
        static List<PixInstance> list = new();

        //记录哪些引用目标下的列表发生了变动
        static Dictionary<GameObject, bool> dirtyDict = new();
        //按instanceTarget为分组的List
        static Dictionary<GameObject, List<PixInstance>> instDict = new();
        //每个instanceTarget的边界盒
        static Dictionary<GameObject, Bounds> boundsDict = new();
        //每个instanceTarget下可能会有多个渲染对象，这里记录每个对象的LocalMatrix
        static Dictionary<GameObject, Matrix4x4[]> localMatricesDict = new();
        //记录每个instanceTarget下有哪些Mesh
        static Dictionary<GameObject, Mesh[]> meshesDict = new();
        //记录每个instanceTarget下有哪些材质，因为一个Mesh可能有多个材质，所以这里是二维数组
        static Dictionary<GameObject, Material[][]> materialsDict = new();
        
        //下面三个是给Shader提交数据用的，焊死只是为了gc友好
        static Matrix4x4[] worldMatrices = new Matrix4x4[1023];
        static Matrix4x4[] preWorldMatrices = new Matrix4x4[1023];
        static MaterialPropertyBlock mpb;

        //这里记录的是每个PixInstance上一帧的localToWorld，又是给TAA用的
        static Dictionary<GameObject, Matrix4x4> targetPreviousMatrixDic = new();

        /// <summary>
        /// 交给渲染管线调用的渲染Pass
        /// 要是这里的passID能用管线里的ShaderTagID作为参数的话会更体面
        /// </summary>
        public static void DrawPass(PixRenderer renderer, int passID, Plane[] frustum)
        {
            if (list.Count < 1) return; //有一个没分组的list就有这好处，判断要不要画时逻辑很简单

            foreach (var kv in instDict) // per target loop
            {
                GameObject tar = kv.Key;

                if (!meshesDict.TryGetValue(tar, out Mesh[] meshes)) continue;
                if (meshes.Length == 0) continue;

                List<PixInstance> insts = kv.Value;

                for (int i = 0; i < meshes.Length; i++) // per mesh loop
                {
                    mpb.Clear();

                    var mesh = meshes[i];
                    var localMatrix = localMatricesDict[tar][i];
                    var mats = materialsDict[tar][i];

                    for (int j = 0; j < mats.Length; j++) // oneMore material oneMore DRAWCALL
                    {
                        int count = 0;
                        foreach (var inst in insts) // combine instance 
                        {
                            if (GeometryUtility.TestPlanesAABB(frustum, inst.bounds))// frustum culling
                            {
                                worldMatrices[count] = inst.transform.localToWorldMatrix * localMatrix;
                                preWorldMatrices[count] = inst._prevLocalToWorld * localMatrix;
                                count++;
                            }
                        }

                        // 老是上一帧上一帧什么TAA什么的，这里就是终点，可算把结果提交给Shader了
                        // 多句嘴，你只要把东西交给commandbuffer提交了，你就别管这东西是值还是引用，后面的修改与这里提交的东西都无关了
                        // 所以我用static的数据在的这里反复蹂躏
                        mpb.SetMatrixArray("_PreviousLocalToWorld", preWorldMatrices);
                        // upload command for drawing
                        renderer.cmb.DrawMeshInstanced(mesh, j, mats[j], passID, worldMatrices, count, mpb);
                    }
                }
            }
        }

        /// <summary>
        /// 交给渲染管线那边在渲染结束后执行,是的，为了TAA
        /// </summary>
        public static void UpdatePrevioursMatrix()
        {
            foreach (var inst in list)
                inst._prevLocalToWorld = inst.transform.localToWorldMatrix;

            var targets = targetPreviousMatrixDic.Keys.ToArray();
            foreach (var t in targets)
                targetPreviousMatrixDic[t] = t.transform.localToWorldMatrix;
        }
        #endregion

        // 要instanc谁
        public GameObject target;
        // 给FrustumCulling用的
        Bounds bounds;
        // 记录之前的target,这样才方便知道target是否发生变动了
        GameObject _prevTarget;
        // 上一帧的localToWorld矩阵，对，TAA，但是也能用它知道坐标是不是发生改变了，要不要刷新bounds
        Matrix4x4 _prevLocalToWorld = Matrix4x4.identity;


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
            if (manager == null)
                manager = this;

            list.Add(this);
            MakeDirty();
        }

        void OnDisable()
        {
            list.Remove(this);
            MakeDirty();

            if (this == manager)
            {
                manager = null;

                if (list.Count > 0)
                    manager = list[0];
                else
                    Clear();
            }
        }

        void Clear()
        {
            instDict.Clear();
            dirtyDict.Clear();
            boundsDict.Clear();
            localMatricesDict.Clear();
            meshesDict.Clear();
            materialsDict.Clear();
            targetPreviousMatrixDic.Clear();
        }

        void Clear(GameObject target)
        {
            instDict.Remove(target);
            dirtyDict.Remove(target);
            boundsDict.Remove(target);
            localMatricesDict.Remove(target);
            meshesDict.Remove(target);
            materialsDict.Remove(target);
            targetPreviousMatrixDic.Remove(target);
        }

        void Update()
        {
            // 在每帧检测自己的transform有没有变换，如果变换了则更新边界盒
            if (transform.localToWorldMatrix != _prevLocalToWorld)
                UpdateBounds();
        }

        // 更新边界盒用来做FrustumCulling
        void UpdateBounds()
        {
            if (!target) return;
            if (!boundsDict.ContainsKey(target) || boundsDict[target] == null) return;

            var localBounds = boundsDict[target];
            bounds = PixUtils.CalculateWorldBounds(transform, localBounds);
        }

        // 在编辑器中如果改变了instancing的target需要刷新
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

        // manager的私人领地，负责按target分组的数据刷新
        void LateUpdate()
        {
            if (this != manager) return;

            mpb ??= new();

            var objs = dirtyDict.Keys.ToArray();

            foreach (var obj in objs)
            {
                if (dirtyDict[obj])
                    RefreshList(obj);
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
                {
                    instDict[obj].Add(inst);
                    inst.UpdateBounds();
                }
            }

            PixInstance component = obj.GetComponent<PixInstance>();
            var rens = obj.GetComponentsInChildren<Renderer>();
            if (instDict[obj].Count == 0)
            {
                Clear(obj);
                if (component)
                {
                    DestroyImmediate(component);
                    foreach (var ren in rens)
                        ren.enabled = true;
                }
                var pixinst = obj.GetComponent<PixInstance>();
                if (pixinst) DestroyImmediate(pixinst);
            }
            else if (!component)
            {
                component = obj.AddComponent<PixInstance>();
                component.SetTarget(obj);
            }

            dirtyDict[obj] = false;
        }

        // 在这里收集instancTarget对象下的相关数据
        // 比如有几个Mesh几个材质，合并后的边界盒之类的
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
                    corners[j] = obj.transform.worldToLocalMatrix.MultiplyPoint3x4(corners[j]);

                boundses[i] = new Bounds(corners[0],Vector3.zero);
                for (int j = 1; j < 8; j++)
                    boundses[i].Encapsulate(corners[j]);

                if (ren is MeshRenderer)
                {
                    if (ren.TryGetComponent(out MeshFilter mf))
                        meshes[i] = mf.sharedMesh;
                }
                else if (ren is SkinnedMeshRenderer) //TODO: fullfil skinnedMesh instancing
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

        // 把多个AABB的Bounds合并成一个大的，总不能让我在画一个PixInstance做FrustumCulling时去Culling多次吧
        Bounds MergeBounds(Bounds[] boundsArray)
        {
            if (boundsArray == null || boundsArray.Length == 0)
                return new Bounds(Vector3.zero, Vector3.zero);

            Bounds mergedBounds = boundsArray[0];
            for (int i = 1; i < boundsArray.Length; i++)
                mergedBounds.Encapsulate(boundsArray[i]);

            return mergedBounds;
        }
    }
}
