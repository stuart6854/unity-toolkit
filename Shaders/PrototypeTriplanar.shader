// World-space triplanar prototype shader for URP.
// The pattern is procedural, so it stays crisp and keeps its size however objects are scaled.
// Lighting: main light (with shadows), ambient and fog. Additional lights are not supported.
Shader "StuMillman/Toolkit/Prototype Triplanar"
{
    Properties
    {
        _BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1)
        [Enum(Grid, 0, Checker, 1, Plain, 2)] _Pattern("Pattern", Float) = 0
        _TileSize("Tile Size (m)", Float) = 1
        _PatternStrength("Pattern Strength", Range(0, 1)) = 1
        _LineWidth("Line Width", Range(0.001, 0.1)) = 0.015
        [Toggle] _MinorLines("Minor Lines (4 per tile)", Float) = 1
        _Smoothness("Smoothness", Range(0, 1)) = 0.2
        _BlendSharpness("Triplanar Blend Sharpness", Range(1, 32)) = 8
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Pattern;
                float _TileSize;
                half _PatternStrength;
                float _LineWidth;
                float _MinorLines;
                half _Smoothness;
                float _BlendSharpness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            // Anti-aliased lines on tile edges. Returns 0..1 coverage.
            float GridLines(float2 uv, float width)
            {
                float2 d = abs(frac(uv + 0.5) - 0.5);   // Distance to nearest line, in tiles.
                float2 aa = max(fwidth(uv), 1e-5);
                float2 l = 1.0 - smoothstep(width - aa, width + aa, d);
                return max(l.x, l.y);
            }

            // Pattern value for one plane: 0 = base colour, 1 = full highlight.
            float PlanePattern(float2 uv)
            {
                if (_Pattern < 0.5)
                {
                    float major = GridLines(uv, _LineWidth);
                    float minor = _MinorLines > 0.5 ? GridLines(uv * 4.0, _LineWidth * 2.0) * 0.4 : 0.0;
                    return max(major, minor);
                }
                if (_Pattern < 1.5)
                {
                    float2 c = floor(uv);
                    return fmod(abs(c.x + c.y), 2.0);
                }
                return 0.0;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                float3 w = pow(abs(n), _BlendSharpness);
                w /= max(w.x + w.y + w.z, 1e-5);

                float3 p = input.positionWS / max(_TileSize, 1e-4);
                float pattern = PlanePattern(p.zy) * w.x + PlanePattern(p.xz) * w.y + PlanePattern(p.xy) * w.z;

                // Grid lines are lighter than the base; checker squares are darker.
                half3 highlight = _Pattern < 0.5 ? saturate(_BaseColor.rgb * 1.6 + 0.08) : _BaseColor.rgb * 0.75;
                half3 albedo = lerp(_BaseColor.rgb, highlight, pattern * _PatternStrength);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half atten = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                half nDotL = saturate(dot(n, mainLight.direction));

                float3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 h = normalize(mainLight.direction + viewDir);
                half spec = pow(saturate(dot(n, h)), exp2(10.0 * _Smoothness + 1.0)) * _Smoothness;

                half3 color = albedo * SampleSH(n)
                            + (albedo * nDotL + spec * nDotL) * mainLight.color * atten;

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // Shadows, depth prepass and SSAO normals reuse URP Lit's passes.
        UsePass "Universal Render Pipeline/Lit/SHADOWCASTER"
        UsePass "Universal Render Pipeline/Lit/DEPTHONLY"
        UsePass "Universal Render Pipeline/Lit/DEPTHNORMALS"
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
