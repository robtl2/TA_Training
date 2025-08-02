using UnityEngine;
using UnityEngine.U2D;

namespace PixRenderPipline
{
    /// <summary>
    /// 贴花组件引用的资源
    /// PixDecal是以SpriteAtlas为分组进行绘制的
    /// 所以这里相当于设置一组贴花的相关参数
    /// </summary>
    [CreateAssetMenu(fileName = "PixDecalAsset", menuName = "Pix/DecalAsset")]
    public class PixDecalAsset : ScriptableObject
    {
        public PixDecal.BlendMode blendMode;

        public SpriteAtlas spriteAtlas;
    }
}
