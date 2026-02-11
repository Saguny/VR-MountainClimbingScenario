Shader "Tutorial/VolumetricFog_FinalFix"
{
    Properties
    {
        _Color("Fog Color", Color) = (1, 1, 1, 1)
        _MaxDistance("Max Distance", float) = 150
        _StepSize("Step Size", Range(0.1, 5)) = 0.5
        _DensityMultiplier("Density", Range(0, 10)) = 1
        _FogNoise("3D Noise", 3D) = "white" {}
        _NoiseTiling("Noise Tiling", float) = 0.1
        _DensityThreshold("Threshold", Range(0, 1)) = 0.1
        [HDR]_LightContribution("Light Intensity", Color) = (1, 1, 1, 1)
        _LightScattering("G (Scattering)", Range(0, 0.98)) = 0.5
    }

    SubShader
    {
        // Use "BeforeTransparent" to ensure we can see it from a distance
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
            
            TEXTURE3D(_FogNoise);
            SAMPLER(sampler_FogNoise);

            float henyey_greenstein(float cosAngle, float g)
            {
                float g2 = g * g;
                // Clamped denominator to prevent "flashing"
                return (1.0 - g2) / (4.0 * PI * pow(max(0.01, 1.0 + g2 - 2.0 * g * cosAngle), 1.5));
            }

            float get_density(float3 worldPos)
            {
                // Multiply worldPos by Tiling - ensures Y (height) is respected
                float3 uvw = worldPos * _NoiseTiling;
                float noise = SAMPLE_TEXTURE3D_LOD(_FogNoise, sampler_FogNoise, uvw, 0).r;
                return saturate(noise - _DensityThreshold) * _DensityMultiplier;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);
                float rawDepth = SampleSceneDepth(IN.texcoord);
                
                // Get Camera-Relative World Positions
                float3 worldPos = ComputeWorldSpacePosition(IN.texcoord, rawDepth, UNITY_MATRIX_I_VP);
                float3 cameraPos = GetCameraPositionWS();
                
                float3 viewVec = worldPos - cameraPos;
                float viewDist = length(viewVec);
                float3 rayDir = viewVec / viewDist;

                // Stop at geometry or MaxDistance
                float distLimit = min(viewDist, _MaxDistance);
                
                // Jittering based on screen pixel to break up banding
                float jitter = InterleavedGradientNoise(IN.texcoord * _ScreenParams.xy, 0);
                float distTravelled = jitter * _StepSize;
                
                float transmittance = 1.0;
                float3 fogLight = 0;

                
                

                // Start Raymarch
                [loop]
                while (distTravelled < distLimit)
                {
                    float3 currentPos = cameraPos + rayDir * distTravelled;
                    float d = get_density(currentPos);

                    if (d > 0.01)
                    {
                        // LIGHTING
                        // Use a slight bias to prevent self-shadowing artifacts
                        float4 shadowCoord = TransformWorldToShadowCoord(currentPos);
                        Light mainLight = GetMainLight(shadowCoord);
                        
                        // God Ray Math
                        float cosAngle = dot(rayDir, mainLight.direction);
                        float phase = henyey_greenstein(cosAngle, _LightScattering);
                        
                        // Attenuation (Shadows + Light Color)
                        float3 lightColor = mainLight.color * mainLight.shadowAttenuation;
                        
                        // Accumulate
                        float3 stepLight = lightColor * _LightContribution.rgb * phase * d;
                        fogLight += stepLight * transmittance * _StepSize;
                        
                        // Beer-Lambert law
                        transmittance *= exp(-d * _StepSize);
                    }

                    if (transmittance < 0.01) break;
                    distTravelled += _StepSize;
                }

                // Blend: Background + Scattered Fog Light
                // We use _Color as a global "Tint" for the fog density
                float3 finalColor = (sceneColor.rgb * transmittance) + (fogLight * _Color.rgb);
                
                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}