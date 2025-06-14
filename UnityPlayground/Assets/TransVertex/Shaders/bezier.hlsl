#define MAX_KNOT_COUNT 64

// 贝塞尔曲线插值函数
float3 BezierInterpolate(float3 p0, float3 p1, float3 p2, float3 p3, float t)
{
    float oneMinusT = 1.0 - t;
    float oneMinusT2 = oneMinusT * oneMinusT;
    float oneMinusT3 = oneMinusT2 * oneMinusT;
    float t2 = t * t;
    float t3 = t2 * t;
    
    return oneMinusT3 * p0 + 
           3.0 * oneMinusT2 * t * p1 + 
           3.0 * oneMinusT * t2 * p2 + 
           t3 * p3;
}

// 贝塞尔曲线导数
float3 BezierDerivative(float3 p0, float3 p1, float3 p2, float3 p3, float t)
{
    float oneMinusT = 1.0 - t;
    float oneMinusT2 = oneMinusT * oneMinusT;
    float t2 = t * t;
    
    return 3.0 * oneMinusT2 * (p1 - p0) + 
           6.0 * oneMinusT * t * (p2 - p1) + 
           3.0 * t2 * (p3 - p2);
}

// 样条曲线评估函数
void EvaluateSpline(
    float percent, 
    float3 points[MAX_KNOT_COUNT],
    float3 normals[MAX_KNOT_COUNT],
    float3 tangentsIn[MAX_KNOT_COUNT],
    float3 tangentsOut[MAX_KNOT_COUNT],
    int knotCount,
    out float3 position, 
    out float3 upVector, 
    out float3 tangent)
{
    // 初始化输出值
    position = float3(0, 0, 0);
    upVector = float3(0, 1, 0);
    tangent = float3(1, 0, 0);
    
    // 如果没有足够的结点，直接返回
    if (knotCount < 2)
        return;
    
    // 计算实际段落索引和局部t值
    float totalPercent = percent * (knotCount - 1);
    int segmentIndex = min(floor(totalPercent), knotCount - 2);
    float localT = frac(totalPercent);
    
    // 获取当前段的控制点
    float3 p0 = points[segmentIndex];
    float3 p1 = tangentsOut[segmentIndex];
    float3 p2 = tangentsIn[segmentIndex + 1];
    float3 p3 = points[segmentIndex + 1];
    
    // 计算位置
    position = BezierInterpolate(p0, p1, p2, p3, localT);
    
    // 计算切线方向
    tangent = normalize(BezierDerivative(p0, p1, p2, p3, localT));
    
    // 计算上向量 - 使用法向量插值并正交化
    float3 normal0 = normals[segmentIndex];
    float3 normal1 = normals[segmentIndex + 1];
    float3 interpolatedNormal = normalize(lerp(normal0, normal1, localT));
    
    // 确保上向量与切线正交
    upVector = normalize(interpolatedNormal - dot(interpolatedNormal, tangent) * tangent);
}