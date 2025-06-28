using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/* --------------------------------------------------------------------------------
 STrail特性：
     . 坐标闪移时，Trail可自动熔断
     . 可自定义Trail截面
     . Trail朝向可选相机或上方
     . 可根据速度或距离来判断Trail是否进行采样
     . 同时提供两套不同的UV(拉伸与稳定)
     . 最高可细分到6级
     
     
     已实现优化点：segment update, pools, gpu catmulRom spline
     可继续优化点：为支持Instancing的机器一次把相同subdivision和材质的Trail画完

                                                                        @wilsonluo
 ----------------------------------------------------------------------------------*/



/// <summary>
/// 可用于飘带、刀光、轨迹、脚印等的Trail工具
/// 配合Shader:OSG/E/UnLit Uber使用
/// </summary>
[ExecuteInEditMode]
public class STrail : MonoBehaviour
{

    /// <summary>
    /// 采样触发方式
    /// </summary>
    public enum TriggerType
    {
        Always,
        Speed,
        Distance,
        Manual,
    }

    /// <summary>
    /// 避免因坐标闪移造成Trail拉扯的自动熔断机制
    /// 当前帧与上一帧之间坐标距离超过此值则自动熔断
    /// 前端需根据具体使用场景赋值
    /// </summary>
    public static float FuseDistance = 10.0f;

    public bool CustomFuseDistance = false;
    public float CustomFuseDistanceValue = 10.0f;

    /// <summary>
    /// 释放STrail用的相关资源池
    /// </summary>
    public static void DisposePool()
    {
        SamplerPool.Dispose();
        MeshSegmentPool.Dispose();
    }

    // 避免帧率不稳产生的疏密采样
    const float UpdateInterval = 1.0f / 60;

    /// <summary>
    /// 采样触发方式
    /// </summary>
    public TriggerType triggerType = TriggerType.Always;

    /// <summary>
    /// 当速度大于此值时，Trail开始采样
    /// </summary>
    public float startSpeed = 10;

    /// <summary>
    /// Trail采样时，当速度小于此值，则结束采样
    /// </summary>
    public float stopSpeed = 1;

    /// <summary>
    /// 与上次采样的距离大于此值时才再次采样
    /// </summary>
    public float distanceThreshold = 0.2f;

    /// <summary>
    /// 手动开关采样
    /// </summary>
    public bool allowSampling = true;

    /// <summary>
    /// Trail的生存时间
    /// </summary>
    public float lifeTime = 1.0f;

    /// <summary>
    /// 对采样路径的细分平滑次数
    /// 1 为关闭平滑
    /// </summary>
    [Range(1, 6)]
    public int subdivision = 2;

    /// <summary>
    /// Trail材质
    /// </summary>
    public Material material;

    /// <summary>
    /// 使用自定义路径截面
    /// </summary>
    public bool customTrailSection;

    /// <summary>
    /// 自定义路径截面 点A
    /// </summary>
    public Transform pointA;

    /// <summary>
    /// 自定义路径截面 点B
    /// </summary>
    public Transform pointB;

    /// <summary>
    /// 界面上显示的speedTrigger的参考参数
    /// </summary>
    public float curSpeed { get; private set; }

    List<Sampler> samplers = new List<Sampler>();
    List<MeshSegment> meshSegments = new List<MeshSegment>();

    Mesh mesh;
    MeshRenderer meshRenderer;//这个只是为了让材质能显示在STrail组件的下面，方便操作

    bool inited = false;
    bool inSampling = false;

    #region 传值到mesh顶点中，在Shader的VS中计算catmullRom的插值坐标
    Vector3[] P0;       //p0
    Vector3[] P1;       //p1
    Vector3[] M0;       //m0
    Vector4[] M1;       //xyz:m1 w:t

    Vector4[] UV;       //x:stretchX y:stableX z:y w:life
    int[] triangles;
    #endregion

    int samplerCount;
    int vertexCount;
    float stableUV = 0;
    float sectionWidth = 1.0f;
    float timeDelta = 0;
    Vector3 lastPos;
    int preVertexCount = 0;

    void OnEnable()
    {
        inited = false;
        Init();
    }

    void OnDisable()
    {
        Clear();
    }

    void OnDestroy()
    {
        if (meshRenderer) meshRenderer.sharedMaterial = null;
    }

    void LateUpdate()
    {
        if (customTrailSection && (!pointA || !pointB)) return;
        if (!inited) Init();

        //提高路径采样点的稳定性
        timeDelta += Time.deltaTime;
        if (timeDelta < UpdateInterval) return;

        //检查是否需要熔断
        float d;
        if (CheckFuse(out d))
        {
            timeDelta = 0;
            return;
        }

        //检查是否允许采样
        switch (triggerType)
        {
            case TriggerType.Always:
                inSampling = true;
                break;
            case TriggerType.Speed:
                curSpeed = d / timeDelta;
                CheckSpeedTrigger(curSpeed);
                break;
            case TriggerType.Distance:
                CheckDistanceTrigger(d);
                break;
            case TriggerType.Manual:
                inSampling = allowSampling;
                break;
        }

        //更新Trail
        UpdateSamplers(timeDelta);
        UpdateMesh();

        timeDelta = 0;
    }

    void Update()
    {
        if (meshSegments.Count < 1) return;

        Graphics.DrawMesh(mesh, Matrix4x4.identity, material, gameObject.layer);
    }
    
    void Init()
    {
        inSampling = false;

        lastPos = getCurrentPos();

        if (customTrailSection && (!pointA || !pointB))
            sectionWidth = 1;
        else
            sectionWidth = customTrailSection ? Vector3.Distance(pointA.position, pointB.position) : 1;

        if (!mesh) mesh = new Mesh();

        //这个只是为了让材质能显示在STrail组件的下面，方便操作
        meshRenderer = GetComponent<MeshRenderer>();
        if (!meshRenderer) meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.hideFlags = HideFlags.HideInInspector;
        meshRenderer.sharedMaterial = material;
        meshRenderer.enabled = false;

        Clear();

        inited = true;
    }

    bool CheckFuse(out float distance)
    {
        Vector3 curPos = getCurrentPos();
        distance = Vector3.Distance(curPos, lastPos);
        lastPos = curPos;

        float fuseDistance = CustomFuseDistance ? CustomFuseDistanceValue : FuseDistance;
        if (fuseDistance == 0) return false;

        if (distance > fuseDistance)
        {
            Clear();
            return true;
        }

        return false;
    }

    void CheckSpeedTrigger(float speed)
    {
        if (!inSampling)
        {
            if (speed > startSpeed) inSampling = true;
        }
        else
        {
            if (speed < stopSpeed) inSampling = false;
        }
    }

    void CheckDistanceTrigger(float distance)
    {

        if (samplers.Count == 0)
            inSampling = true;
        else
            inSampling = distance > distanceThreshold;
    }

    void UpdateSamplers(float timeDelta)
    {

        #region 移除生命周期结束的采样点与mesh顶点
        samplerCount = samplers.Count;

        for (int i = 0; i < samplerCount; ++i)
            samplers[i].life += timeDelta;

        if (meshSegments.Count > 0)
        {
            var lastSampler = samplers[samplers.Count - 1];
            var lastSegment = meshSegments[meshSegments.Count - 1];

            if (lastSampler.life > lifeTime)
            {
                samplers.Remove(lastSampler);
                SamplerPool.Release(lastSampler);

                meshSegments.Remove(lastSegment);
                MeshSegmentPool.Release(lastSegment);
            }

            if (meshSegments.Count == 0) stableUV = 0;
        }
        #endregion


        #region 新增一个采样点，并更新trail开端的两段顶点(MeshSegment存在的目的就是为了能少更新点数据就少更新点)
        if (inSampling)
        {

            Sampler sampler = SamplerPool.Get();

            if (customTrailSection)
                sampler.Setup(pointA.position, pointB.position);
            else
                sampler.Setup(transform.position, transform.position);

            samplers.Insert(0, sampler);

            samplerCount = samplers.Count;

            if (samplerCount < 2) return;

            MeshSegment firstSegment;
            if (samplerCount == 2)
            {
                firstSegment = MeshSegmentPool.Get(subdivision);
                firstSegment.Update(samplers[0], samplers[0], samplers[1], samplers[1]);
                firstSegment.BuildStableUV(sectionWidth, ref stableUV);
                meshSegments.Insert(0, firstSegment);
                return;
            }

            var sampler_3 = samplerCount == 3 ? samplers[2] : samplers[3];
            meshSegments[0].Update(samplers[0], samplers[1], samplers[2], sampler_3);

            firstSegment = MeshSegmentPool.Get(subdivision);
            firstSegment.Update(samplers[0], samplers[0], samplers[1], samplers[2]);
            firstSegment.BuildStableUV(sectionWidth, ref stableUV);
            meshSegments.Insert(0, firstSegment);
        }
        #endregion

        UpdateStretchUV();

        UpdateSegmentsLife();
    }

    /// <summary>
    /// 再给个把x一直保持从0拉伸到1的UV，目前忽略了每段Trail的不同长度，后续可改良
    /// </summary>
    void UpdateStretchUV()
    {
        int sCount = meshSegments.Count;
        if (sCount < 1) return;

        int totleSegment = sCount * subdivision;
        float rcpTotleSegment = 1.0f / totleSegment;
        float t = 0;
        float x;
        int indexStart;
        for (int i = 0; i < sCount; ++i)
        {
            var meshSegment = meshSegments[i];
            for (int j = 0; j < subdivision; ++j)
            {
                indexStart = j * 2;

#if UNITY_EDITOR
                //避免在editor里运行时修改subdivision产生的error log
                if (indexStart > meshSegment.UV.Length - 1) return;
#endif

                x = t * rcpTotleSegment;
                t++;

                meshSegment.UV[indexStart].x = x;
                meshSegment.UV[indexStart + 1].x = x;
            }
        }
    }

    /// <summary>
    /// 细分后每个顶点当前normalize后的生命值，Shader里透明度用的
    /// 感觉可能用不上，还在考虑要不要保留
    /// </summary>
    void UpdateSegmentsLife()
    {
        int sCount = meshSegments.Count;
        if (sCount < 1) return;

        float life;
        int indexStart;
        for (int i = 0; i < sCount; ++i)
        {
            var meshSegment = meshSegments[i];

            for (int j = 0; j < subdivision; ++j)
            {
                indexStart = j * 2;

#if UNITY_EDITOR
                //避免在editor里运行时修改subdivision产生的error log
                if (indexStart > meshSegment.M1.Length - 1) return;
#endif
                life = Mathf.Lerp(meshSegment.samplerA.life, meshSegment.samplerB.life, meshSegment.M1[indexStart].w);
                life /= lifeTime;
                meshSegment.UV[indexStart].w = life;
                meshSegment.UV[indexStart + 1].w = life;
            }
        }
    }

    void UpdateMesh()
    {
        mesh.Clear(false);
        if (meshSegments.Count < 1) return;

        #region combine vertexs from meshSegments
        int segmentCount = meshSegments.Count;
        int subCount = subdivision * 2;

        vertexCount = subCount * segmentCount;

        P0 = new Vector3[vertexCount];
        P1 = new Vector3[vertexCount];
        M0 = new Vector3[vertexCount];
        M1 = new Vector4[vertexCount];
        UV = new Vector4[vertexCount];

        int vFrom;
        for (int i = 0; i < segmentCount; ++i)
        {
            vFrom = subCount * i;
            var meshSegment = meshSegments[i];

#if UNITY_EDITOR
            //避免在editor里运行时修改subdivision产生的error log
            if (vFrom + meshSegment.P0.Length > P0.Length) return;
#endif

            meshSegment.P0.CopyTo(P0, vFrom);
            meshSegment.P1.CopyTo(P1, vFrom);
            meshSegment.M0.CopyTo(M0, vFrom);
            meshSegment.M1.CopyTo(M1, vFrom);
            meshSegment.UV.CopyTo(UV, vFrom);
        }

        mesh.vertices = P0;
        mesh.SetUVs(1, P1.ToList());
        mesh.normals = M0;
        mesh.tangents = M1;
        mesh.SetUVs(0, UV.ToList());
        #endregion

        #region update triangles
        if (preVertexCount == vertexCount) {
            mesh.triangles = triangles;
            return;
        } 
        preVertexCount = vertexCount;

        int tLoop = meshSegments.Count * subdivision - 1;
        int tFrom;
        triangles = new int[tLoop * 6];
        for (int i = 0; i < tLoop; ++i)
        {
            vFrom = i * 2;
            tFrom = i * 6;

            triangles[tFrom] = vFrom + 3;
            triangles[tFrom + 1] = vFrom + 1;
            triangles[tFrom + 2] = vFrom;

            triangles[tFrom + 3] = vFrom + 3;
            triangles[tFrom + 4] = vFrom + 0;
            triangles[tFrom + 5] = vFrom + 2;
        }
        mesh.triangles = triangles;
        #endregion


    }


    void Clear()
    {
        stableUV = 0;
        mesh.Clear(false);

        foreach (var sampler in samplers) SamplerPool.Release(sampler);
        foreach (var meshSegment in meshSegments) MeshSegmentPool.Release(meshSegment);

        samplers.Clear();
        meshSegments.Clear();
    }

    Vector3 getCurrentPos()
    {
        if (customTrailSection && (!pointA || !pointB)) return transform.position;

        Vector3 pos = customTrailSection ? (pointA.position + pointB.position) * 0.5f : transform.position;
        return pos;
    }


#if UNITY_EDITOR
    void OnValidate()
    {
        Init();
    }
#endif

    /// <summary>
    /// 坐标采样点
    /// </summary>
    class Sampler
    {
        public Vector3 posA { get; private set; }
        public Vector3 posB { get; private set; }
        public Vector3 center { get; private set; }

        public float life = 0;

        public void Setup(Vector3 posA, Vector3 posB)
        {
            this.posA = posA;
            this.posB = posB;

            center = (posA + posB) * 0.5f;
            life = 0;
        }
    }

    /// <summary>
    /// 两个sampler之间的Mesh顶点数据
    /// </summary>
    class MeshSegment
    {
        public int segment { get; private set; }
        public float length { get; private set; }
        public Vector3[] P0 { get; private set; }
        public Vector3[] P1 { get; private set; }
        public Vector3[] M0 { get; private set; }
        public Vector4[] M1 { get; private set; }

        /// <summary>
        /// 同时保存两套UV
        /// x:stretchX  y:stableX z:y w:life
        /// </summary>
        public Vector4[] UV { get; private set; }

        public int vertexCount { get; private set; }

        public Sampler samplerA { get; private set; }
        public Sampler samplerB { get; private set; }

        Vector3 p0, m0, p1;
        Vector4 m1;
        float pointStep;
        public MeshSegment(int segment)
        {
            this.segment = segment;

            vertexCount = segment * 2;
            P0 = new Vector3[vertexCount];
            M0 = new Vector3[vertexCount];
            P1 = new Vector3[vertexCount];
            M1 = new Vector4[vertexCount];
            UV = new Vector4[vertexCount];
        }

        /// <summary>
        /// 更新顶点数据
        /// </summary>
        public void Update(Sampler sampler0, Sampler sampler1, Sampler sampler2, Sampler sampler3)
        {
            length = Vector3.Distance(sampler1.center, sampler2.center);
            samplerA = sampler1;
            samplerB = sampler2;

            pointStep = 1.0f / segment;

            for (int i = 0; i < 2; ++i)
            {
                bool isA = i == 0;
                if (isA)
                {
                    p0 = sampler1.posA;
                    p1 = sampler2.posA;

                    m0 = sampler2.posA - sampler0.posA;
                    m1 = sampler3.posA - sampler1.posA;
                }
                else
                {
                    p0 = sampler1.posB;
                    p1 = sampler2.posB;

                    m0 = sampler2.posB - sampler0.posB;
                    m1 = sampler3.posB - sampler1.posB;
                }

                m0 *= 0.5f;
                m1 *= 0.5f;
                m1.w = 0;

                P0[i] = p0;
                P1[i] = p1;
                M0[i] = m0;
                M1[i] = m1;
                UV[i].z = i;

                if (segment == 1) continue;

                int index;
                for (int t = 1; t < segment; ++t)
                {
                    m1.w = t * pointStep;
                    index = t * 2 + i;

                    P0[index] = p0;
                    P1[index] = p1;
                    M0[index] = m0;
                    M1[index] = m1;
                    UV[index].z = i;
                }
            }
        }

        /// <summary>
        /// 提供一套一但生成就不再变化的稳定的UV
        /// </summary>
        public void BuildStableUV(float width, ref float u)
        {
            int startIndex;
            pointStep *= length / width;
            for (int i = segment - 1; i >= 0; --i)
            {
                startIndex = i * 2;

                UV[startIndex].y = u;
                UV[startIndex + 1].y = u;

                u += pointStep;
            }
        }
    }

    #region Pools for avoid gc
    class SamplerPool
    {
        static Stack<Sampler> pool = new Stack<Sampler>();

        public static Sampler Get()
        {
            if (pool.Count == 0)
                return new Sampler();
            else
                return pool.Pop();
        }

        public static void Release(Sampler sampler)
        {
            if (pool.Count > 0 && pool.Peek() == sampler) return;

            pool.Push(sampler);
        }

        public static void Dispose()
        {
            pool.Clear();
        }
    }

    class MeshSegmentPool
    {

        /// <summary>
        /// 不同段数的MeshSegment分别缓存
        /// </summary>
        static Dictionary<int, Stack<MeshSegment>> pools = new Dictionary<int, Stack<MeshSegment>>();

        public static MeshSegment Get(int segment)
        {
            if (!pools.ContainsKey(segment)) return new MeshSegment(segment);

            var pool = pools[segment];

            if (pool.Count == 0)
                return new MeshSegment(segment);
            else
                return pool.Pop();
        }

        public static void Release(MeshSegment meshSegment)
        {
            if (!pools.ContainsKey(meshSegment.segment))
                pools[meshSegment.segment] = new Stack<MeshSegment>();

            var pool = pools[meshSegment.segment];

            if (pool.Count > 0 && pool.Peek() == meshSegment) return;

            pool.Push(meshSegment);
        }

        public static void Dispose()
        {
            foreach (int segment in pools.Keys)
                pools[segment].Clear();

            pools.Clear();
        }
    }

    #endregion
}
