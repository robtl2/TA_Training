using UnityEngine;

namespace PixRenderPipline{

    public static class PixUtils
    {
        static Vector3[] worldCorners = new Vector3[8];
        public static Bounds CalculateWorldBounds(Transform transform, Bounds localBounds)
        {
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
                worldCorners[i] = transform.localToWorldMatrix.MultiplyPoint3x4(localCorners[i]);

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

        public static Vector3[] GetBoundsCorners(Bounds bounds)
        {
            Vector3[] corners = new Vector3[8];
            corners[0] = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z); // 左下后
            corners[1] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z); // 右下后
            corners[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z); // 左上后
            corners[3] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z); // 右上后
            corners[4] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z); // 左下前
            corners[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z); // 右下前
            corners[6] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z); // 左上前
            corners[7] = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z); // 右上前

            return corners;
        }
    }
}