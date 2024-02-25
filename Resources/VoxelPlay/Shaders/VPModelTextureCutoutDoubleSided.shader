Shader "Voxel Play/Models/Texture/Cutout Double Sided"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
		[HDR] _Color ("Color", Color) = (1,1,1,1)
        _CustomDaylightShadowAtten ("Daylight Shadow Atten", Range(0,1)) = 0.65
        [HideInInspector] _TintColor ("Color", Color) = (1,1,1,1)
		_VoxelLight ("Voxel Light", Range(0,15)) = 15
		[Toggle(_EMISSION)]
		_UseEmission ("Use Emission", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}
	}

	SubShader {

		Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" "RenderPipeline" = "UniversalPipeline"  }
		Pass {
			Tags { "LightMode" = "UniversalForward" }
			Cull Off
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex   vert
			#pragma fragment frag
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile _ VOXELPLAY_GLOBAL_USE_FOG
            #pragma multi_compile _ VOXELPLAY_GPU_INSTANCING
			#pragma multi_compile_instancing nolightprobe nolodfade
			#if UNITY_VERSION < 202100
				#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
				#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#else
				#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
			#endif
			#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
			#pragma shader_feature _EMISSION
			#if UNITY_VERSION >= 202200
				#pragma multi_compile _ _FORWARD_PLUS
			#endif
			#define SUBTLE_SELF_SHADOWS
			#define USE_TEXTURE
			#define NON_ARRAY_TEXTURE
            #define VP_CUTOUT
            #include "VPCommonURP.cginc"
            #include "VPCommonCore.cginc"
			#include "VPModel.cginc"
			ENDHLSL
		}

		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			Cull Off
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
			#pragma target 3.5
			#pragma vertex vert
			#pragma fragment frag
		    #pragma multi_compile_instancing
			#define USE_TEXTURE
			#define NON_ARRAY_TEXTURE
            #define VP_CUTOUT
			#include "VPModelShadowsURP.cginc"
			ENDHLSL
		}

		UsePass "Voxel Play/Voxels/Triangle/Opaque/DepthOnly"
		UsePass "Voxel Play/Voxels/Triangle/Opaque/DepthNormalsOnly"

	}

	SubShader {

		Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" }
		Pass {
			Tags { "LightMode" = "ForwardBase" }
			Cull Off
			CGPROGRAM
			#pragma target 3.5
			#pragma vertex   vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_fwdbase nolightmap nodynlightmap novertexlight nodirlightmap
			#pragma multi_compile _ VOXELPLAY_GLOBAL_USE_FOG
            #pragma multi_compile _ VOXELPLAY_GPU_INSTANCING
			#pragma multi_compile_instancing nolightprobe nolodfade
			#pragma shader_feature _EMISSION
			#define SUBTLE_SELF_SHADOWS
			#define USE_TEXTURE
			#define NON_ARRAY_TEXTURE
            #define VP_CUTOUT
            #include "VPCommon.cginc"
			#include "VPModel.cginc"
			ENDCG
		}

		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			Cull Off
			CGPROGRAM
			#pragma target 3.5
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_shadowcaster
			#pragma multi_compile_instancing
			#pragma fragmentoption ARB_precision_hint_fastest
			#define USE_TEXTURE
            #define NON_ARRAY_TEXTURE
            #define VP_CUTOUT
			#include "VPModelShadows.cginc"
			ENDCG
		}
	}

	Fallback Off
}