using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.Rendering.RendererUtils;
using Unity.Mathematics;

/// <summary>
/// 如果renderer相当于写菜单，那Pass就相当于写每道菜的制作流程
/// </summary>
public class PixPassBase
{
    #region 这里放的都是做菜时常用的配料
    static Mesh _fullScreenQuad;
    public Mesh FullScreenQuad
    {
        get
        {
            if (_fullScreenQuad == null)
            {
                _fullScreenQuad = new Mesh
                {
                    vertices = new Vector3[] {
                        new(-1, -1, 0),
                        new(1, -1, 0),
                        new(-1, 1, 0),
                        new(1, 1, 0)
                    },
                    uv = new Vector2[] {
                        new(0, 0),
                        new(1, 0),
                        new(0, 1),
                        new(1, 1)
                    },
                    triangles = new int[] {
                        0, 1, 2,
                        2, 1, 3
                    }
                };
            }
            return _fullScreenQuad;
        }
    }

    static Mesh _tiledFullScreenQuad;
    public Mesh TiledFullScreenQuad
    {
        get
        {
            if (_tiledFullScreenQuad == null)
            {
                int2 tile = renderer.size / 8;
                _tiledFullScreenQuad = new Mesh();

                int tilesX = tile.x;
                int tilesY = tile.y;

                // 计算总顶点数和三角形数
                int vertexCount = (tilesX + 1) * (tilesY + 1);
                int quadCount = tilesX * tilesY;
                int triangleCount = quadCount * 2; // 每个quad有2个三角形

                // 创建数组
                Vector3[] vertices = new Vector3[vertexCount];
                Vector2[] uvs = new Vector2[vertexCount];
                Vector2[] tileCoords = new Vector2[vertexCount]; // 额外的UV集用于tile坐标
                int[] triangles = new int[triangleCount * 3]; // 每个三角形3个索引

                // 生成顶点和UV
                for (int y = 0; y <= tilesY; y++)
                {
                    for (int x = 0; x <= tilesX; x++)
                    {
                        int index = y * (tilesX + 1) + x;

                        // 顶点坐标 (-1,-1) 到 (1,1)
                        float normalizedX = (float)x / tilesX;
                        float normalizedY = (float)y / tilesY;
                        vertices[index] = new Vector3(-1 + normalizedX * 2, -1 + normalizedY * 2, 0);

                        // UV坐标 (0,0) 到 (1,1)
                        uvs[index] = new Vector2(normalizedX, normalizedY);

                        // Tile坐标 - 这将标识每个顶点属于哪个tile
                        tileCoords[index] = new Vector2(x, y);
                    }
                }

                // 生成三角形索引
                int triangleIndex = 0;
                for (int y = 0; y < tilesY; y++)
                {
                    for (int x = 0; x < tilesX; x++)
                    {
                        // 计算当前quad的四个顶点索引
                        int bottomLeft = y * (tilesX + 1) + x;
                        int bottomRight = bottomLeft + 1;
                        int topLeft = (y + 1) * (tilesX + 1) + x;
                        int topRight = topLeft + 1;

                        // 第一个三角形 (顺时针): 左下 -> 右下 -> 左上
                        triangles[triangleIndex++] = bottomLeft;
                        triangles[triangleIndex++] = bottomRight;
                        triangles[triangleIndex++] = topLeft;

                        // 第二个三角形 (顺时针): 左上 -> 右下 -> 右上
                        triangles[triangleIndex++] = topLeft;
                        triangles[triangleIndex++] = bottomRight;
                        triangles[triangleIndex++] = topRight;
                    }
                }

                // 设置网格数据
                _tiledFullScreenQuad.vertices = vertices;
                _tiledFullScreenQuad.uv = uvs;
                _tiledFullScreenQuad.SetUVs(1, tileCoords); // UV1通道存储tile坐标
                _tiledFullScreenQuad.triangles = triangles;
            }
            return _tiledFullScreenQuad;
        }
    }

    protected static Color black = new(0, 0, 0, 0);
    #endregion

    
    public PixRenderer renderer { get; private set; }
    public  string passName { get; private set; }
    public PixPassBase(string passName, PixRenderer renderer)
    {
        this.renderer = renderer;
        this.passName = passName;
    }

    public virtual void Execute()
    {
        renderer.cmb.name = passName;
    }

    #region 这里放一些做菜时常用的技法
    public void GetTemporaryColorRT(int nameID, int width, int height)
    {
        renderer.cmb.GetTemporaryRT(nameID, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGB32, renderer.colorSpace);
    }

    public void GetTemporaryColorRT(int nameID)
    {
        GetTemporaryColorRT(nameID, renderer.size.x, renderer.size.y);
    }

    /// <summary>
    /// 获取渲染列表
    /// </summary>
    /// <param name="tag">Shader的Tags中写的LightMode名字</param>
    /// <param name="sortingCriteria">排序方式</param>
    /// <param name="renderQueueRange">渲染队列范围</param>
    public RendererList GetRendererList(ShaderTagId tag, SortingCriteria sortingCriteria, RenderQueueRange renderQueueRange)
    {
        if (renderer.cullingSuccess)
        {
            RendererListDesc rendererListDesc = new(tag, renderer.cullingResults, renderer.camera)
            {
                renderQueueRange = renderQueueRange,
                sortingCriteria = sortingCriteria
            };

            RendererList rendererList = renderer.context.CreateRendererList(rendererListDesc);
            return rendererList;
        }

        //如果renderer那边剔除都没过，那这里就return一个肯定isValid为false的list
        RendererListDesc invalidDesc = new();
        RendererList invalidRendererList = renderer.context.CreateRendererList(invalidDesc);
        return invalidRendererList;
    }

    #endregion
}