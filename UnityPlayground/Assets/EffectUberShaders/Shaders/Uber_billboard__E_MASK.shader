Shader "Hidden/Uber/billboard/_E_MASK"
{
	Properties
    {
        rule_1("S(_E_DISSOLVE)/_E_FLIPBOOK",Int) = 0

        PACKED_SHADER_1("SEffect/UnLit Uber Base|Hidden/Uber/|__|_E_ADD_TEX,_E_MASK,_E_DISTORTION,_E_DISSOLVE,_E_RIM,_E_FLIPBOOK",Int) = 3
        PACKED_SHADER_2("Hidden/Uber/billboard/base|Hidden/Uber/billboard/|_E_BILLBOARD|_E_ADD_TEX,_E_MASK,_E_DISTORTION,_E_DISSOLVE,_E_FLIPBOOK",Int) = 2
        PACKED_SHADER_3("Hidden/Uber/fullscreen/base|Hidden/Uber/fullscreen/|_E_FULLSCREEN|_E_ADD_TEX,_E_MASK,_E_DISTORTION,_E_DISSOLVE",Int) = 2
        PACKED_SHADER_4("Hidden/Uber/strail/base|Hidden/Uber/strail/|STRAIL|_E_ADD_TEX,_E_MASK,_E_DISTORTION,_E_DISSOLVE",Int) = 2
    

        Group#Feature("特性", Int) = 1
            _CurBlendMode("B/混合模式:_BlendSrc,_BlendDst[Feature]",Int) = 0
            _VertexFn("G(__,_E_BILLBOARD,_E_FULLSCREEN,STRAIL)/特殊功能:无,广告牌,全屏显示,STrail[Feature]",Int) = 0 
            _FlipbookToggle("C(_E_FLIPBOOK)/UV序列图[Feature]", Int) = 0
            _AddTexToggle("C(_E_ADD_TEX)/叠加贴图[Feature]", Int) = 0
            _MaskToggle("C(_E_MASK)/遮罩[Feature]", Int) = 0
            _DistortionToggle("C(_E_DISTORTION)/扰动[Feature]", Int) = 0
            _DissolveToggle("C(_E_DISSOLVE)/溶解[Feature]", Int) = 0
            _RimToggle("C(_E_RIM)/边缘光[Feature]", Int) = 0


        Group#Main("基本参数", Int) = 1
            _ZOffset("屏幕深度偏移[Main]",Range(-1,1)) = 0
            _MainTex("主贴图[Main]", 2D) = "white" {}
            _MainCoordsys("E/坐标系:拉伸,稳定[Main,STRAIL]",Int) = 0
            _MainRollUV("V/UV滚动:U向滚动,V向滚动[Main]", Vector) = (0, 0, 0, 0)
            [HDR]_Color("叠加色[Main]", Color) = (1, 1, 1, 1)
            _MainAlpha("调整透明度[Main]", Float) = 1 


            //packed properties below
        Group#_E_MASK("遮罩[_E_MASK]", Int) = 1
            _MaskChannel("E/通道:R,G,B,A[_E_MASK]", Int) = 0
            _Mask_Alpha("T/影响透明度[_E_MASK]",Int) = 1
            _Mask_Color("T/影响叠加色[_E_MASK]",Int) = 1
            _MaskTex("遮罩贴图[_E_MASK]", 2D) = "white" {}
            _MaskRollUV("V/UV滚动:U向滚动,V向滚动[_E_MASK]", Vector) = (0, 0, 0, 0)
        Group#_E_BILLBOARD("广告牌[_E_BILLBOARD]",Int) = 1
            _BillBoard_Y("T/只操作Y轴[_E_BILLBOARD]",Int) = 0
            //-----------------------

        
        Group#Others("其它", Int) = 0
            _ZWrite("T/深度写入[Others]", Int) = 0
            _CullMode("E/Cull:Off,Front,Back[Others]", Int) = 2
            _ZTestMode("E/ZTest:Disable,Never,Less,Equal,LessEqual,Greater,NotEqual,GreaterEqual,Always[Others]", Float) = 4
            _BlendSrc("__BlendSrc", Int) = 5
            _BlendDst("__BlendDst", Int) = 10

    } 

    SubShader
    {
        Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" "PreviewType"="Plane"}
        LOD 100
        Cull[_CullMode]
        Blend[_BlendSrc][_BlendDst]
        ZWrite[_ZWrite]
        ZTest[_ZTestMode]
        
        Pass
        {

            CGPROGRAM

            //packed properties below
            #define _E_MASK
            #define _E_BILLBOARD
            //-----------------------

            #include "cginc/uber.cginc"

            #pragma vertex vert
            #pragma fragment frag

            ENDCG
        }
    }

    CustomEditor "SShaderGUI"
}
