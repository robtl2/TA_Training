using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PixRenderPipline
{

    [ExecuteInEditMode]
    public class PixSky : MonoBehaviour
    {
        public static PixSky instance { get; private set; }
        public Material material { get; private set; }
        public enum SkyType
        {
            None,
            Color,
            Texture,
            Procedural,
        }

        public SkyType skyType = SkyType.None;
        public Color displayColor;
        public Color color;

        public int detherStep = 0;
        public Cubemap texture;

        public PixSHData shData = new();
        [Range(0, 1)]
        public float IrradianceColor = 0;

        [Range(0, 3)]
        public float intensity = 1;

        [Range(0, 3.5f)]
        public float IrradianceIntensity = 1.0f;

        [Range(0, 9)]
        public float blurLevel = 0;

        [Range(0, 3)]
        public float fovScale = 1;

        readonly int _SkyType = Shader.PropertyToID("_SkyType");
        readonly int _SkyColor = Shader.PropertyToID("_SkyColor");
        readonly int _SkyDisplayColor = Shader.PropertyToID("_SkyDisplayColor");
        readonly int _Dethering = Shader.PropertyToID("_Dethering");
        readonly int _FovScale = Shader.PropertyToID("_FovScale");
        readonly int _RotateSky = Shader.PropertyToID("_RotateSky");
        readonly int _SkyTex = Shader.PropertyToID("_SkyTex");
        readonly int _BlurLevel = Shader.PropertyToID("_BlurLevel");
        readonly int _SkyTexMipCount = Shader.PropertyToID("_SkyTexMipCount");
        readonly int _SkySH = Shader.PropertyToID("_SkySH");
        readonly int _IrradianceColor = Shader.PropertyToID("_IrradianceColor");
        readonly int _IrradianceIntensity = Shader.PropertyToID("_IrradianceIntensity");

        void OnEnable()
        {
            instance = this;
            if (material == null)
                material = new Material(Shader.Find("Hidden/Pix/Sky"));
        }

        void OnDisable()
        {
            instance = null;
        }

        void Update()
        {
            if (this != instance) return;
            if (skyType == SkyType.None) return;

            material.SetInt(_SkyType, (int)skyType - 1);

            material.SetFloat(_FovScale, fovScale);
            var yRotation = GetYRotationInRadians();

            material.SetFloat(_BlurLevel, blurLevel);
            material.SetColor(_SkyDisplayColor, displayColor);
            material.SetInt(_Dethering, detherStep);

            Shader.SetGlobalColor(_SkyColor, color * intensity);
            Shader.SetGlobalFloat(_SkyTexMipCount, texture ? texture.mipmapCount : 0);
            Shader.SetGlobalFloat(_RotateSky, yRotation);
            Shader.SetGlobalVectorArray(_SkySH, shData.shCoefficients);
            Shader.SetGlobalFloat(_IrradianceColor, IrradianceColor);
            Shader.SetGlobalFloat(_IrradianceIntensity, IrradianceIntensity);

            if (skyType == SkyType.Texture && texture != null)
                Shader.SetGlobalTexture(_SkyTex, texture);
        }

        // 将 transform 的 y 轴旋转转换为 -π 到 π 之间的弧度值
        float GetYRotationInRadians()
        {
            float yRotationDegrees = transform.eulerAngles.y;
            float yRotationRadians = yRotationDegrees * Mathf.Deg2Rad;

            if (yRotationRadians > Mathf.PI)
                yRotationRadians -= 2 * Mathf.PI;

            return yRotationRadians;
        }
    }
    
    [System.Serializable]
    public class PixSHData
    {
        public Vector4[] shCoefficients = new Vector4[9];

#if UNITY_EDITOR

        public static void DirToCubemapFaceUV(Vector3 dir, out CubemapFace face, out Vector2 uv)
        {
            dir.Normalize();
            float absX = Mathf.Abs(dir.x);
            float absY = Mathf.Abs(dir.y);
            float absZ = Mathf.Abs(dir.z);

            bool isXPositive = dir.x > 0;
            bool isYPositive = dir.y > 0;
            bool isZPositive = dir.z > 0;

            float u = 0, v = 0;

            if (absX >= absY && absX >= absZ)
            {
                // X轴主导
                if (isXPositive)
                {
                    face = CubemapFace.PositiveX;
                    u = -dir.z / absX;
                    v = -dir.y / absX;
                }
                else
                {
                    face = CubemapFace.NegativeX;
                    u = dir.z / absX;
                    v = -dir.y / absX;
                }
            }
            else if (absY >= absX && absY >= absZ)
            {
                // Y轴主导
                if (isYPositive)
                {
                    face = CubemapFace.PositiveY;
                    u = dir.x / absY;
                    v = dir.z / absY;
                }
                else
                {
                    face = CubemapFace.NegativeY;
                    u = dir.x / absY;
                    v = -dir.z / absY;
                }
            }
            else
            {
                // Z轴主导
                if (isZPositive)
                {
                    face = CubemapFace.PositiveZ;
                    u = dir.x / absZ;
                    v = -dir.y / absZ;
                }
                else
                {
                    face = CubemapFace.NegativeZ;
                    u = -dir.x / absZ;
                    v = -dir.y / absZ;
                }
            }

            uv = new Vector2(0.5f * (u + 1.0f), 0.5f * (v + 1.0f));
        }

        public void Bake(Cubemap cubemap)
        {
            var isReadable = cubemap.isReadable;

            // 如果cubemap的importer的read/write enabled为false，则需要重新设置为true
            if (!isReadable)
            {
                var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(cubemap)) as TextureImporter;
                if (importer != null)
                {
                    importer.isReadable = true;
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(cubemap));
                }
            }

            const int SH_SAMPLE_COUNT = 1000; // 采样点数量
            const int MIP_LEVEL = 6; // 使用第6级mipmap

            // 初始化SH系数
            for (int i = 0; i < 9; i++)
                shCoefficients[i] = Vector4.zero;

            // Monte Carlo积分计算SH系数
            for (int i = 0; i < SH_SAMPLE_COUNT; i++)
            {
                // 在球面上均匀采样
                float u = Random.value * 2.0f - 1.0f;
                float phi = Random.value * 2.0f * Mathf.PI;
                float theta = Mathf.Acos(u);

                // 计算方向向量
                Vector3 dir = new Vector3(
                    Mathf.Sin(theta) * Mathf.Cos(phi),
                    Mathf.Sin(theta) * Mathf.Sin(phi),
                    Mathf.Cos(theta)
                );

                // 从cubemap采样
                DirToCubemapFaceUV(dir, out CubemapFace face, out Vector2 uv);
                Color color = cubemap.GetPixel(face, (int)(uv.x * cubemap.width), (int)(uv.y * cubemap.height), MIP_LEVEL);
                Vector4 colorVec = new Vector4(color.r, color.g, color.b, 1.0f);

                // 计算球谐基函数值 (2阶需要9个系数)
                float Y00 = 0.282095f;
                float Y1_1 = 0.488603f * dir.y;
                float Y10 = 0.488603f * dir.z;
                float Y11 = 0.488603f * dir.x;
                float Y2_2 = 1.092548f * dir.x * dir.y;
                float Y2_1 = 1.092548f * dir.y * dir.z;
                float Y20 = 0.315392f * (3.0f * dir.z * dir.z - 1.0f);
                float Y21 = 1.092548f * dir.x * dir.z;
                float Y22 = 0.546274f * (dir.x * dir.x - dir.y * dir.y);

                // 累加SH系数
                shCoefficients[0] += Y00 * colorVec;
                shCoefficients[1] += Y1_1 * colorVec;
                shCoefficients[2] += Y10 * colorVec;
                shCoefficients[3] += Y11 * colorVec;
                shCoefficients[4] += Y2_2 * colorVec;
                shCoefficients[5] += Y2_1 * colorVec;
                shCoefficients[6] += Y20 * colorVec;
                shCoefficients[7] += Y21 * colorVec;
                shCoefficients[8] += Y22 * colorVec;
            }

            // 归一化系数
            float factor = 4.0f * Mathf.PI / SH_SAMPLE_COUNT;
            for (int i = 0; i < 9; i++)
                shCoefficients[i] *= factor;

            if (!isReadable)
            {
                var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(cubemap)) as TextureImporter;
                if (importer != null)
                {
                    importer.isReadable = false;
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(cubemap));
                }
            }

            Debug.Log("Baking SH data from cubemap: " + cubemap.name);
        }
#endif
    }

    
        
}