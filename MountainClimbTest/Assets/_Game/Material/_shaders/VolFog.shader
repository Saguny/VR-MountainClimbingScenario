Shader "Tutorial/VolumetricFog_FinalFix"
{
    Properties
    {
        _Color("Fog Color", Color) = (1, 1, 1, 1)
        _MaxDistance("Max Distance", float) = 100
        _StepSize("Step Size", Range(0.1, 5)) = 1.0
        _DensityMultiplier("Density", Range(0, 10)) = 1
        _FogNoise("3D Noise", 3D) = "white" {}
        _NoiseTiling("Noise Tiling", float) = 0.1
        _DensityThreshold("Threshold", Range(0, 1)) = 0.1
        [HDR]_LightContribution("Light Intensity", Color) = (1, 1, 1, 1)
        _LightScattering("G (Scattering)", Range(0, 0.98)) = 0.5
        _HeightFalloff("Height Falloff", Range(0, 1)) = 0.5
        _BaseHeight("Base Height", float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+1" }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 _Color;
            float _MaxDistance;
            float _DensityMultiplier;
            float _StepSize;
            float _NoiseTiling;
            float _DensityThreshold;
            float _LightScattering;
            float4 _LightContribution;
            float _HeightFalloff;
            float _BaseHeight;
            
            TEXTURE3D(_FogNoise);
            SAMPLER(sampler_FogNoise);

            float henyey_greenstein(float cosAngle, float g)
            {
                float g2 = g * g;
                return (1.0 - g2) / (4.0 * PI * pow(max(0.01, 1.0 + g2 - 2.0 * g * cosAngle), 1.5));
            }

            float get_density(float3 worldPos)
            {
                // Height-based falloff for more realistic fog
                float heightFactor = saturate(exp(-max(0, worldPos.y - _BaseHeight) * _HeightFalloff));
                
                float3 uvw = worldPos * _NoiseTiling;
                float noise = SAMPLE_TEXTURE3D_LOD(_FogNoise, sampler_FogNoise, uvw, 0).r;
                float density = saturate(noise - _DensityThreshold) * _DensityMultiplier;
                
                return density * heightFactor;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);
                float rawDepth = SampleSceneDepth(IN.texcoord);
                
                float3 worldPos = ComputeWorldSpacePosition(IN.texcoord, rawDepth, UNITY_MATRIX_I_VP);
                float3 cameraPos = GetCameraPositionWS();
                
                float3 viewVec = worldPos - cameraPos;
                float viewDist = length(viewVec);
                float3 rayDir = viewVec / viewDist;

                bool isSkybox = rawDepth >= 0.9999;
                float distLimit = isSkybox ? _MaxDistance : min(viewDist, _MaxDistance);
                
                float jitter = InterleavedGradientNoise(IN.texcoord * _ScreenParams.xy, 0);
                float distTravelled = jitter * _StepSize;
                
                float transmittance = 1.0;
                float3 fogLight = 0;

                Light mainLight = GetMainLight();

                // Raymarch
                [loop]
                while (distTravelled < distLimit)
                {
                    float3 currentPos = cameraPos + rayDir * distTravelled;
                    float d = get_density(currentPos);

                    if (d > 0.01)
                    {
                        float4 shadowCoord = TransformWorldToShadowCoord(currentPos);
                        float shadow = MainLightRealtimeShadow(shadowCoord);
                        
                        // God rays - only visible when looking toward light
                        float cosAngle = dot(rayDir, mainLight.direction);
                        float phase = henyey_greenstein(cosAngle, _LightScattering);
                        
                        // Subtle light scattering
                        float3 lightColor = mainLight.color * shadow;
                        float3 stepLight = lightColor * _LightContribution.rgb * phase * d * 0.5; // Reduced multiplier
                        
                        fogLight += stepLight * transmittance * _StepSize;
                        transmittance *= exp(-d * _StepSize);
                    }

                    if (transmittance < 0.01) break;
                    distTravelled += _StepSize;
                }

                // Gentle fog blend
                float3 finalColor = sceneColor.rgb * transmittance + fogLight;
                
                // Apply fog color only to dense areas
                float fogAmount = 1.0 - transmittance;
                finalColor = lerp(finalColor, _Color.rgb, fogAmount * 0.3);
                
                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}