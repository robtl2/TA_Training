using UnityEngine;
using UnityEngine.U2D;

namespace PixRenderPipline
{
    [CreateAssetMenu(fileName = "PixDecalAsset", menuName = "Pix/DecalAsset")]
    public class PixDecalAsset : ScriptableObject
    {
        public enum BlendMode
        {
            Transparent,
            Additive,
            Multiply
        }

        public BlendMode blendMode;

        public SpriteAtlas spriteAtlas;
    }
}
