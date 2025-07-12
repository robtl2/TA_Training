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
    public Cubemap texture;

    [Range(0, 9)]
    public float blurLevel = 0;
    [Range(0, 3)]
    public float intensity = 1;

    [Range(0, 3)]
    public float fovScale = 1;

    int _SkyType = Shader.PropertyToID("_SkyType");
    int _Color = Shader.PropertyToID("_Color");
    int _FovScale = Shader.PropertyToID("_FovScale");
    int _RotateSky = Shader.PropertyToID("_RotateSky");
    int _SkyTex = Shader.PropertyToID("_SkyTex");
    int _BlurLevel = Shader.PropertyToID("_BlurLevel");
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
        var yRotation = GetYRotationInRadians();
        material.SetFloat(_RotateSky, yRotation);
        material.SetFloat(_BlurLevel, blurLevel);

        if (skyType == SkyType.Texture && texture != null)
            material.SetTexture(_SkyTex, texture);

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
