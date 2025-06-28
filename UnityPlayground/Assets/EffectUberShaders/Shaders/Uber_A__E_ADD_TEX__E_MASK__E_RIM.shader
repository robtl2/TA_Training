Shader "Hidden/Uber_A/_E_ADD_TEX__E_MASK__E_RIM"
{
    Properties
    {

        PACKED_SHADER("SEffect/UnLit Uber Advance Base|Hidden/Uber_A/|__|_E_ADD_TEX,_E_MASK,_E_DISTORTION,_E_DISSOLVE,_E_RIM",Int) = 3


        Group#Feature("特性", Int) = 1  
            _CurBlendMode("B/混合模式:_BlendSrc,_BlendDst[Feature]",Int) = 0 
            _AddTexToggle("C(_E_ADD_TEX)/叠加贴图[Feature]", Int) = 0
            _MaskToggle("C(_E_MASK)/遮罩[Feature]", Int) = 0
            _DistortionToggle("C(_E_DISTORTION)/扰动[Feature]", Int) = 0
            _DissolveToggle("C(_E_DISSOLVE)/溶解[Feature]", Int) = 0
            _RimToggle("C(_E_RIM)/边缘光[Feature]", Int) = 0

        Group#Main("基本参数", Int) = 1 
            _ZOffset("屏幕深度偏移[Main]",Range(-1,1)) = 0
            _Main_ClampUV("T/关闭贴图重复[Main]",Float) = 0
            _MainTex("主贴图[Main]", 2D) = "white" {}
            _MainCoordsys("E/坐标系:模型UV,极坐标,屏幕坐标,matcap[Main]",Int) = 0
            _MainRollUV("V/UV滚动:U向滚动,V向滚动,固定旋转,匀速旋转[Main,!_MainCoordsys_3]", Vector) = (0, 0, 0, 0)
            _Space(" /[Main]", Float) = 0
            _UseAlpha("T/使用A通道作为透明度[Main]",Float) = 1
            _MainAlpha("调整透明度[Main]", Float) = 1 
            _AsGray("去色[Main]",Range(0,1)) = 0
            [HDR]_Color("叠加色[Main]", Color) = (1, 1, 1, 1)

            //packed properties below
        Group#_E_ADD_TEX("叠加贴图[_E_ADD_TEX]", Int) = 1
            _AddTexBlendMode("E/混合模式:Multiply,AlphaBlend,add[_E_ADD_TEX]", Int) = 0
            _Add_ClampUV("T/关闭贴图重复[_E_ADD_TEX]",Int) = 0
            _AddTex("叠加贴图[_E_ADD_TEX]", 2D) = "white" {}
            _AddCoordsys("E/坐标系:模型UV,极坐标,屏幕坐标,matcap[_E_ADD_TEX]",Int) = 0
            _DistortionByMainColor("UV被主贴图亮度影响[_E_ADD_TEX]",Float) = 0
            _AddRollUV("V/UV滚动:U向滚动,V向滚动,固定旋转,匀速旋转[_E_ADD_TEX,!_AddCoordsys_3]", Vector) = (0, 0, 0, 0)
            [HDR]_AddColor("叠加色[_E_ADD_TEX]", Color) = (1, 1, 1, 1)
        Group#_E_MASK("遮罩[_E_MASK]", Int) = 1
            _Mask_AddTex("T/只遮罩叠加贴图[_E_MASK,_E_ADD_TEX]",Int) = 1
            _MaskChannel("E/通道:R,G,B,A[_E_MASK]", Int) = 0
            _Mask_ClampUV("T/关闭贴图重复[_E_MASK]",Int) = 0
            _Mask_Alpha("T/影响透明度[_E_MASK]",Int) = 1
            _Mask_Color("T/影响叠加色[_E_MASK]",Int) = 1
            _MaskTex("遮罩贴图[_E_MASK]", 2D) = "white" {}
            _MaskCoordsys("E/坐标系:模型UV,极坐标,屏幕坐标,matcap[_E_MASK]",Int) = 0
            _MaskRollUV("V/UV滚动:U向滚动,V向滚动,固定旋转,匀速旋转[_E_MASK,!_MaskCoordsys_3]", Vector) = (0, 0, 0, 0)
            _Space(" /[_E_MASK]", Int) = 1
            _MaskInt("遮罩强度[_E_MASK]", Range(0, 2)) = 1
            _MaskFea("羽化过渡[_E_MASK]", Range(0, 1)) = 1
        Group#_E_RIM("边缘光[_E_RIM]", Int) = 1
            _RimGhost("T/Ghost[_E_RIM]",Int) = 0
            _ReverseGhost("T/模型边缘虚化[_E_RIM,_RimGhost]",Float) = 0
            [HDR]_RimColor("颜色[_E_RIM]", Color) = (1, 1, 1, 1)
            _RimOffset("偏移[_E_RIM]",Range(0,1)) = 0
            //-----------------------


               
        Group#Others("其它", Int) = 0 
            _ZWrite("T/深度写入[Others]", Int) = 0
            _CullMode("E/Cull:Off,Front,Back[Others]", Int) = 2
            _ZTestMode("E/ZTest:Disable,Never,Less,Equal,LessEqual,Greater,NotEqual,GreaterEqual,Always[Others]", Int) = 4
            _BlendSrc("__BlendSrc", Int) = 5
            _BlendDst("__BlendDst", Int) = 10

    }


    SubShader
    {
        Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" "PreviewType"="Plane"}

        Cull[_CullMode]
        Blend[_BlendSrc][_BlendDst]
        ZWrite [_ZWrite]
        ZTest[_ZTestMode]


        LOD 100   

        Pass
        {

            CGPROGRAM
            #define _E_ADVANCE
            
            //packed properties below
            #define _E_ADD_TEX
            #define _E_MASK
            #define _E_RIM
            //-----------------------
         
            #include "cginc/uber.cginc"

            #pragma vertex vert
            #pragma fragment frag

            ENDCG
        }
    }


    CustomEditor "SShaderGUI"
}
