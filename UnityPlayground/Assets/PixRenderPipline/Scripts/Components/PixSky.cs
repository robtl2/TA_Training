using UnityEngine;

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
    public Color color;
    public Texture2D texture;

    [Range(-3.1416f, 3.1416f)]
    public float rotateSky = 0;

    [Range(0, 3)]
    public float intensity = 1;

    [Range(0, 3)]
    public float fovScale = 1;

    int _SkyType = Shader.PropertyToID("_SkyType");
    int _Color = Shader.PropertyToID("_Color");
    int _FovScale = Shader.PropertyToID("_FovScale");
    int _RotateSky = Shader.PropertyToID("_RotateSky");
    int _SkyTex = Shader.PropertyToID("_SkyTex");

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
        material.SetColor(_Color, color * intensity);
        material.SetFloat(_FovScale, fovScale);
        material.SetFloat(_RotateSky, rotateSky);

        if (skyType == SkyType.Texture && texture != null)
            material.SetTexture(_SkyTex, texture);

    }
}
