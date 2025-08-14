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
        static Dictionary<GameObject, bool> dirtyDict = new();


        public GameObject target;

        GameObject _prevTarget;

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

            instDict[obj].Clear();

            foreach (var inst in list)
            {
                if (inst.target == obj)
                    instDict[obj].Add(inst);
            }

            dirtyDict[obj] = false;
        }
    }
}
