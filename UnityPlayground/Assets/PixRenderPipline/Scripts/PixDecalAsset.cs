using UnityEngine;
using UnityEngine.U2D;

namespace PixRenderPipline
{
    [CreateAssetMenu(fileName = "PixDecalAsset", menuName = "Pix/DecalAsset")]
    public class PixDecalAsset : ScriptableObject
    {
        public PixDecal.BlendMode blendMode;

        public SpriteAtlas spriteAtlas;
    }
}
