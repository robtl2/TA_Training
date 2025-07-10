using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[ExecuteInEditMode]
public class BlobShadow : MonoBehaviour
{
    const int MAX_BLOBS = 64;
    static BlobShadow blobMan;
    static readonly List<BlobShadow> blobs = new();
    static readonly Vector4[] blobRects = new Vector4[MAX_BLOBS];
    static readonly float[] blobHeights = new float[MAX_BLOBS];

    public float radius = 1;
    public float heightOffset = 0;

    void OnEnable()
    {
        if (blobMan == null)
            blobMan = this;

        blobs.Add(this);
    }

    void OnDisable()
    {
        blobs.Remove(this);

        if (blobMan == this)
        {
            blobMan = null;

            // blobMan被关了的话，其它blob就再抢一次
            BlobShadow[] blobArray = blobs.ToArray(); //先复制一份出来再遍历，防患于未然
            foreach (var blob in blobArray)
            {
                blob.OnDisable();
                blob.OnEnable();
            }
        }
    }

    Vector4 CalculateRect()
    {
        float4 rect = new(0, 0, 0, 0);
        float3 pos = new(transform.position);
        float2 center = new(pos.x, pos.z);
        float2 size = new(radius, radius);

        rect.xy = center - size * 0.5f;
        rect.zw = size;

        return new Vector4(rect.x, rect.y, rect.z, rect.w);
    }

    void Update()
    {
        if (blobMan != this)
            return;

        int count = math.min(blobs.Count, MAX_BLOBS);

        for (int i = 0; i < count; i++)
        {
            var blob = blobs[i];
            blobRects[i] = blob.CalculateRect();
            blobHeights[i] = blob.transform.position.y + blob.heightOffset;
        }

        Shader.SetGlobalVectorArray("_BlobRects", blobRects);
        Shader.SetGlobalFloatArray("_BlobHeights", blobHeights);
        Shader.SetGlobalInt("_BlobCount", count);
    }
}
