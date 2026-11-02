// Water surface of the global map: procedural waves, sky fresnel, sun glints and animated surf along the coast.
// The coast proximity map and the map rectangle are provided at runtime by MapWaterView.
Shader "LearnLanguages/MapWater"
{
    Properties
    {
        _DeepColor ("Deep color", Color) = (0.06, 0.33, 0.58, 0.82)
        _ShallowColor ("Shallow color", Color) = (0.14, 0.6, 0.7, 0.55)
        _SkyColor ("Sky reflection", Color) = (0.78, 0.9, 1, 1)
        _FoamColor ("Foam color", Color) = (1, 1, 1, 0.9)
        _SeabedColor ("Seabed color seen through the water", Color) = (0.537, 0.886, 1, 1)
        [NoScaleOffset] _CoastMap ("Coast proximity (runtime)", 2D) = "black" {}
        _MapRect ("Map rectangle: x, z, width, depth (runtime)", Vector) = (0, 0, 1, 1)
        _WaveScale ("Wave scale", Float) = 0.45
        _WaveSpeed ("Wave speed", Float) = 0.35
        _WaveStrength ("Wave normal strength", Range(0, 2)) = 0.6
        _SpecularPower ("Specular power", Float) = 400
        _SpecularIntensity ("Specular intensity", Float) = 0.6
        _ShallowWidth ("Shallow width", Range(0.05, 1)) = 0.6
        _FoamWidth ("Shore foam width", Range(0.01, 0.5)) = 0.08
        _SurfLines ("Surf lines", Float) = 4
        _SurfSpeed ("Surf speed", Float) = 0.25
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-50" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "MapWater"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_CoastMap);
            SAMPLER(sampler_CoastMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _ShallowColor;
                half4 _SkyColor;
                half4 _FoamColor;
                half4 _SeabedColor;
                float4 _MapRect;
                float _WaveScale;
                float _WaveSpeed;
                float _WaveStrength;
                float _SpecularPower;
                float _SpecularIntensity;
                float _ShallowWidth;
                float _FoamWidth;
                float _SurfLines;
                float _SurfSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

            float2 Hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453);
            }

            float GradientNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = dot(Hash2(i), f);
                float b = dot(Hash2(i + float2(1, 0)), f - float2(1, 0));
                float c = dot(Hash2(i + float2(0, 1)), f - float2(0, 1));
                float d = dot(Hash2(i + float2(1, 1)), f - float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Three wave trains moving in different directions.
            float Waves(float2 p, float t)
            {
                return GradientNoise(p + float2(t, t * 0.6)) * 0.55
                     + GradientNoise(p * 2.13 + float2(-t * 0.9, t * 0.45)) * 0.3
                     + GradientNoise(p * 4.71 + float2(t * 1.4, -t * 1.1)) * 0.15;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 positionWS = input.positionWS;
                float t = _Time.y * _WaveSpeed;
                float2 p = positionWS.xz * _WaveScale;

                const float e = 0.08;
                float h = Waves(p, t);
                float hx = Waves(p + float2(e, 0), t);
                float hz = Waves(p + float2(0, e), t);
                float3 normal = normalize(float3((h - hx) / e * _WaveStrength, 1.0, (h - hz) / e * _WaveStrength));

                float2 mapUv = (positionWS.xz - _MapRect.xy) / _MapRect.zw;
                float inside = step(0.0, mapUv.x) * step(mapUv.x, 1.0) * step(0.0, mapUv.y) * step(mapUv.y, 1.0);
                float coast = SAMPLE_TEXTURE2D(_CoastMap, sampler_CoastMap, saturate(mapUv)).r * inside;

                half4 water = lerp(_DeepColor, _ShallowColor, smoothstep(1.0 - _ShallowWidth, 1.0, coast));

                Light light = GetMainLight();
                float3 viewDir = normalize(GetWorldSpaceViewDir(positionWS));
                float diffuse = 0.6 + 0.4 * saturate(dot(normal, light.direction));
                float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), 5.0);
                float3 halfDir = normalize(light.direction + viewDir);
                float specular = pow(saturate(dot(normal, halfDir)), _SpecularPower) * _SpecularIntensity;

                float3 color = water.rgb * diffuse * light.color;
                color = lerp(color, _SkyColor.rgb, saturate(fresnel * 0.35));
                color += specular * light.color;

                // Surf: a solid band on the shoreline and broken lines rolling towards it.
                float foamNoise = GradientNoise(positionWS.xz * 0.9 + t * 0.5) * 0.5 + 0.5;
                float shore = smoothstep(1.0 - _FoamWidth, 1.0, coast + (foamNoise - 0.5) * _FoamWidth);
                float surfPhase = frac(coast * _SurfLines - _Time.y * _SurfSpeed);
                float surf = smoothstep(0.85, 0.95, surfPhase) * (1.0 - smoothstep(0.95, 1.0, surfPhase));
                surf *= smoothstep(0.35, 0.8, coast) * step(0.45, foamNoise);
                float foam = saturate(shore + surf * 0.35) * _FoamColor.a;
                color = lerp(color, _FoamColor.rgb, foam);

                float alpha = saturate(max(water.a + fresnel * 0.3 + specular, foam));
                // Beyond the map there is no seabed under the water: it is mixed in here, so the map has no visible edge.
                float2 edge = min(mapUv, 1.0 - mapUv);
                float beyond = 1.0 - smoothstep(0.0, 0.04, min(edge.x, edge.y));
                color = lerp(color, color * alpha + _SeabedColor.rgb * diffuse * light.color * (1.0 - alpha), beyond);
                alpha = lerp(alpha, 1.0, beyond);
                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
