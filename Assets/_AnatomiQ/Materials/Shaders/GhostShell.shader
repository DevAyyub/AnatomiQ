// AnatomiQ/GhostShell — CORE-002 chunk 3.
// A faint, unlit, Fresnel-driven transparent shell for AQ_BodyShell. Near-invisible face-on so the
// organs and (on device) the live camera feed stay clean; alpha ramps up only at grazing/silhouette
// angles, giving a "ghost body" rim instead of a flat grey film over the feed.
//
// Render setup rationale:
//  - Transparent queue: draws AFTER the opaque organs, so it composites over them as a faint overlay.
//  - ZWrite Off + ZTest LEqual: it does not own depth; the opaque organs do, so they occlude correctly.
//  - Cull Back: only the near shell faces draw (a single rim), which minimises muddying.
//  - Unlit: no lighting cost — cheap for the mobile/thermal budget (relevant to the chunk-4 tier work).
Shader "AnatomiQ/GhostShell"
{
    Properties
    {
        _BaseColor        ("Tint", Color)                    = (0.80, 0.85, 0.95, 1)
        _FresnelPower     ("Fresnel Power", Range(0.5, 8))   = 3.0
        _FresnelIntensity ("Fresnel Intensity", Range(0, 1)) = 0.6
        _BaseAlpha        ("Base Alpha (face-on)", Range(0, 0.3)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "GhostShellForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _FresnelPower;
                float  _FresnelIntensity;
                float  _BaseAlpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmInputs = GetVertexNormalInputs(IN.normalOS);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = nrmInputs.normalWS;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 normalWS  = normalize(IN.normalWS);
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(IN.positionWS));

                // Fresnel: 0 when looking straight at a face, ->1 at grazing angles (the silhouette).
                float ndotv   = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - ndotv, _FresnelPower);

                float alpha = saturate(_BaseAlpha + fresnel * _FresnelIntensity);
                return half4(_BaseColor.rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
