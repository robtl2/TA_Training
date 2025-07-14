using UnityEngine;
using System.Collections.Generic;
using UnityEngine.U2D;
using Unity.Mathematics;

namespace PixRenderPipline
{

    [ExecuteInEditMode]
    public class PixDecal : MonoBehaviour
    {
        public static List<PixDecal> decals = new();
        public static Dictionary<SpriteAtlas, List<PixDecal>> decalsBySpriteAtlas = new();

        static Mesh _mesh;
        public static Mesh mesh
        {
            get
            {
                if (_mesh == null)
                {
                    var tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    _mesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
                    DestroyImmediate(tempCube);
                }
                return _mesh;
            }
        }

        public enum DecalShadingModel
        {
            Unlit,
            Lit,
        }

        public DecalShadingModel shadingModel;
        public PixDecalAsset asset;
        public string spriteName;

        [Range(0, 1)]
        public float alpha = 1;
        public uint order = 0;

        /// <summary>
        /// 获取世界空间包围盒，如果transform发生变化会重新计算
        /// </summary>
        public Bounds WorldBounds
        {
            get
            {
                if (boundsDirty)
                {
                    cachedWorldBounds = CalculateWorldBounds();
                    boundsDirty = false;
                }
                return cachedWorldBounds;
            }
        }

        /// <summary>
        /// 获取UV参数，如果spriteAtlas或spriteName发生变化会重新计算
        /// </summary>
        public float4 UV_ST
        {
            get
            {
                if (uvDirty)
                {
                    cachedUV_ST = CalculateUV_ST();
                    uvDirty = false;
                }
                return cachedUV_ST;
            }
        }

        /// <summary>
        /// 获取对应的纹理
        /// </summary>
        public Texture2D Texture
        {
            get
            {
                return GetTexture();
            }
        }

        // 包围盒缓存
        Vector3[] worldCorners = new Vector3[8];
        Bounds cachedWorldBounds;
        bool boundsDirty = true;
        Vector3 lastPosition;
        Quaternion lastRotation;
        Vector3 lastScale;

        // UV参数缓存
        float4 cachedUV_ST;
        bool uvDirty = true;
        SpriteAtlas lastSpriteAtlas;
        string lastSpriteName;

        void OnEnable()
        {
            if (asset.spriteAtlas == null) return;

            decals.Add(this);

            if (!decalsBySpriteAtlas.ContainsKey(asset.spriteAtlas))
                decalsBySpriteAtlas[asset.spriteAtlas] = new List<PixDecal>();

            decalsBySpriteAtlas[asset.spriteAtlas].Add(this);
            
            // 初始化时标记为需要重新计算
            boundsDirty = true;
            uvDirty = true;
        }

        void OnDisable()
        {
            if (asset.spriteAtlas == null) return;

            decals.Remove(this);

            if (decalsBySpriteAtlas.ContainsKey(asset.spriteAtlas))
                decalsBySpriteAtlas[asset.spriteAtlas].Remove(this);
        }

        void Update()
        { 
            // 检查transform是否发生变化
            if (transform.position != lastPosition || 
                transform.rotation != lastRotation || 
                transform.localScale != lastScale)
            {
                boundsDirty = true;
                lastPosition = transform.position;
                lastRotation = transform.rotation;
                lastScale = transform.localScale;
            }

            if (asset == null) return;

            // 检查spriteAtlas或spriteName是否发生变化
            if (asset.spriteAtlas != lastSpriteAtlas || spriteName != lastSpriteName)
            {
                uvDirty = true;
                lastSpriteAtlas = asset.spriteAtlas;
                lastSpriteName = spriteName;
            }
        }

        void OnDrawGizmosSelected()
        {
            if (!enabled) return;

            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(-Vector3.forward, Vector3.forward);
        }

        /// <summary>
        /// 计算精确的世界空间包围盒
        /// 考虑了transform的scale和rotation，避免过早剔除
        /// </summary>
        Bounds CalculateWorldBounds()
        {
            var localBounds = mesh.bounds;
            
            // 获取local bounds的8个顶点
            Vector3[] localCorners = new Vector3[8];
            localCorners[0] = new Vector3(localBounds.min.x, localBounds.min.y, localBounds.min.z);
            localCorners[1] = new Vector3(localBounds.max.x, localBounds.min.y, localBounds.min.z);
            localCorners[2] = new Vector3(localBounds.min.x, localBounds.max.y, localBounds.min.z);
            localCorners[3] = new Vector3(localBounds.max.x, localBounds.max.y, localBounds.min.z);
            localCorners[4] = new Vector3(localBounds.min.x, localBounds.min.y, localBounds.max.z);
            localCorners[5] = new Vector3(localBounds.max.x, localBounds.min.y, localBounds.max.z);
            localCorners[6] = new Vector3(localBounds.min.x, localBounds.max.y, localBounds.max.z);
            localCorners[7] = new Vector3(localBounds.max.x, localBounds.max.y, localBounds.max.z);

            // 转换到世界空间
            for (int i = 0; i < 8; i++)
                worldCorners[i] = transform.TransformPoint(localCorners[i]);

            // 计算世界空间包围盒
            Vector3 min = worldCorners[0];
            Vector3 max = worldCorners[0];
            
            for (int i = 1; i < 8; i++)
            {
                min = Vector3.Min(min, worldCorners[i]);
                max = Vector3.Max(max, worldCorners[i]);
            }

            Vector3 size = max - min;
            Vector3 center = (min + max) * 0.5f;
            
            return new Bounds(center, size);
        }

        /// <summary>
        /// 计算UV参数
        /// </summary>
        float4 CalculateUV_ST()
        {
            if (asset == null) return new float4(1, 1, 0, 0);

            if (asset.spriteAtlas == null || string.IsNullOrEmpty(spriteName))
                return new float4(1, 1, 0, 0);

            var sprite = asset.spriteAtlas.GetSprite(spriteName);
            if (sprite == null)
                return new float4(1, 1, 0, 0);

            var rect = sprite.textureRect;
            var texture = sprite.texture;
            float2 size = new(texture.width, texture.height);
            
            return new float4(rect.width, rect.height, rect.x, rect.y) / new float4(size, size);
        }

        /// <summary>
        /// 获取对应的纹理
        /// </summary>
        Texture2D GetTexture()
        {
            if (asset == null) return Texture2D.whiteTexture;

            if (asset.spriteAtlas == null || string.IsNullOrEmpty(spriteName))
                return Texture2D.whiteTexture;

            var sprite = asset.spriteAtlas.GetSprite(spriteName);
            return sprite != null ? sprite.texture : Texture2D.whiteTexture;
        }
        
    }
        
}