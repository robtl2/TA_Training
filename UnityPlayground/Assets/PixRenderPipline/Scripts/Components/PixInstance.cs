using System.Collections.Generic;
using UnityEngine;

namespace PixRenderPipline
{
    // TODO: fullfil
    public class PixInstance : MonoBehaviour
    {
        public static PixInstance instMan;
        public static List<PixInstance> list;
        public static Dictionary<GameObject, List<PixInstance>> instDict = new();

        static Dictionary<GameObject, Bounds> boundsDict = new();
        static Dictionary<GameObject, Matrix4x4[]> localMatricesDict = new();
        static Dictionary<GameObject, Mesh[]> meshesDict = new();
        static Dictionary<GameObject, Material[]> materialsDict = new();
        static Dictionary<GameObject, bool> dirtyDict = new();


        public GameObject target { get; private set; }

        public Bounds bounds { get; private set; }

        GameObject _prevTarget;

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
                    instDict.Clear();
                    dirtyDict.Clear();
                }
            }
        }

        void Update()
        {
            UpdateBounds();   
        }

        void UpdateBounds()
        {
            if (!target) return;
            if (!boundsDict.ContainsKey(target) || boundsDict[target] == null) return;

            Vector3[] localCorners = new Vector3[8];
            var localBounds = boundsDict[target];
            int index = 0;
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        localCorners[index++] = new Vector3(
                            localBounds.center.x + ((i == 0) ? -1 : 1) * localBounds.extents.x,
                            localBounds.center.y + ((j == 0) ? -1 : 1) * localBounds.extents.y,
                            localBounds.center.z + ((k == 0) ? -1 : 1) * localBounds.extents.z
                        );
                    }
                }
            }

            bounds = new(localCorners[0], Vector3.zero);
            for (int i = 1; i < localCorners.Length; i++)
                bounds.Encapsulate(transform.TransformPoint(localCorners[i]));
        }

        void OnValidate()
        {
            if (target != _prevTarget)
            {
                MakeDirty();
                _prevTarget = target;
            }
        }

        void MakeDirty()
        {
            if (_prevTarget) dirtyDict[_prevTarget] = true;
            if (target) dirtyDict[target] = true;
        }

        void LateUpdate()
        {
            if (this != instMan) return;

            foreach (var kv in dirtyDict)
            {
                if (kv.Value)
                    RefreshList(kv.Key);
            }
        }

        void RefreshList(GameObject obj)
        {
            if (!instDict.ContainsKey(obj))
                instDict[obj] = new();

            RefreshRefrence(obj);

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
            Renderer[] rens = obj.GetComponentsInChildren<Renderer>();

            int count = rens.Length;
            Mesh[] meshes = new Mesh[count];
            Material[] materials = new Material[count];
            Matrix4x4[] matrices = new Matrix4x4[count];
            Bounds[] bounds = new Bounds[count];
            Matrix4x4 worldToLocal = obj.transform.worldToLocalMatrix;

            for (int i = 0; i < count; i++)
            {
                var ren = rens[i];

                bounds[i] = ren.bounds;

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

                Material mat = ren.sharedMaterial;
                mat.enableInstancing = true;
                materials[i] = mat;

                Matrix4x4 matrixWorld = ren.transform.localToWorldMatrix;
                Matrix4x4 matrixLocal = worldToLocal * matrixWorld;
                matrices[i] = matrixLocal;
            }

            boundsDict[obj] = MergeBounds(bounds);
            meshesDict[obj] = meshes;
            localMatricesDict[obj] = matrices;
            materialsDict[obj] = materials;
        }

        Bounds MergeBounds(Bounds[] boundsArray)
        {
            if (boundsArray == null || boundsArray.Length == 0)
                return new Bounds(Vector3.zero, Vector3.zero);

            Bounds result = boundsArray[0];
            for (int i = 1; i < boundsArray.Length; i++)
                result.Encapsulate(boundsArray[i]);

            return result;
        }
    }
}
