Shader "Clouds/Noise from Image"
{
    Properties
    {
        _Scale ("Scale", Range(0.1, 10.0)) = 2.0
        _StepScale ("Step Size Scale", Range(0.1, 1.0)) = 1.0
        _NumSteps ("Number of Steps", Range(1, 500)) = 100 
        _MinHeight ("Min Height", Range(0.0, 100.0)) = 0.0   
        _MaxHeight ("Max Height", Range(0.0, 100.0)) = 10.0  
        _FadeDist ("Fade Distance", Range(0.0, 10.0)) = 0.5 
        _MoveDir ("Movement Direction", Vector) = (1, 0, 0)
        _SunDir ("Sun Direction", Vector) = (0, 1, 0)
        
        _ValueNoise("Value Noise", 2D) = "white" {}
    }
    SubShader
    {
        // In the rendering queue, transparent objects are rendered last.
        // We can make use of the depth buffer in the fragment shader.
        Tags { "Queue"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off        // Disable backface culling, which is enabled by default
        Lighting Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION; 
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float3 worldPos : TEXCOORD2;    // World space position
                float3 viewDir : TEXCOORD0;     // World space view direction vector
                float4 clipPos : SV_POSITION;   // Camera clip space position 
                float4 scrPos : TEXCOORD1;      // Screen space position
            };

            float _MinHeight;
            float _MaxHeight;
            float _FadeDist;
            float _Scale;
            float _StepScale;
            float _NumSteps;
            float3 _MoveDir;
            float3 _SunDir;

            sampler2D _CameraDepthTexture;

            sampler2D _ValueNoise;

            // This is our vertex shader function.
            // It runs on the vertices of the object, converts them into clip space, and performs other calculations.
            v2f vert (appdata v)
            {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;    // Object space pos --> World space pos
                o.viewDir = o.worldPos - _WorldSpaceCameraPos;          // View direction is from camera to vertex
                o.clipPos = UnityObjectToClipPos(v.vertex);             // Object space pos --> Camera clip space pos
                o.scrPos = ComputeScreenPos(o.clipPos);                // Camera clip space pos --> Screen space pos
                return o;
            }

            // Given a float3, this function retrieves a noise value from a value noise texture.
            float noiseFromImage(float3 x)
            {
                x /= _Scale;
                x += _Time * _MoveDir;

                float3 p = floor(x);
                float3 f = frac(x);
                f = smoothstep(0, 1, f);
                
                float2 uv = (p.xy + float2(37.0, -17.0) * p.z) + f.xy;
                float2 rg = tex2Dlod(_ValueNoise, float4(uv / 256, 0, 0)).rg;
                return -1.0 + 2.0 * lerp(rg.g, rg.r, f.z);
            }

            // This function performs the lighting integration for the cloud color value.
            // It is invoked during each raymarch step where there is cloud density.
            fixed4 integrate(
                fixed4 prevColor, 
                float diffuse, 
                float density, 
                fixed4 bgColor, 
                float depth
            )
            {
                // Factor in density
                fixed4 color;
                color.rgb = lerp(
                    fixed3(1.0, 0.95, 0.8),     // Color for denser clouds
                    fixed3(0.65, 0.65, 0.65),   // Color for less dense clouds
                    density
                );
                color.a = density;
                
                // Factor in lighting/shadows from the sun
                fixed3 lighting = 
                    fixed3(0.65, 0.68, 0.7) * 1.3               // Ambient lighting
                    + 0.5 * fixed3(0.7, 0.5, 0.3) * diffuse;    // Sunlight diffused in clouds
                color.rgb *= lighting;

                // Factor in depth.
                color.rgb = lerp(
                    color.rgb, 
                    bgColor.rgb, 
                    1.0 - exp(-0.003 * depth * depth)   
                );

                color.a *= 0.5;
                color.rgb *= color.a;
                return prevColor + (color * (1.0 - prevColor.a));
            }

            // This is our ray-marching function.
            // MARCH's arguments are passed as references, since it is a macro function.
            // This allows us to output the cloudColor value to the fragment shader.
            #define MARCH( \
                noiseMap, \
                rayOrigin, \ 
                normRayDir, \
                maxDepth, \
                bgColor, \
                cloudColor \  // This value will be passed out
            ) { \
                float depth = 0; \  // Current marched depth
                \ 
                \ // Iterate for each step
                for (int i = 0; i < _NumSteps; i++) \
                { \
                    \ // Break if the maximum depth is reached
                    if (depth > maxDepth) \
                        break; \
                    \
                    \ // Break if the cloud is fully opaque
                    if (cloudColor.a > 0.99) \
                        break; \
                    \
                    \ // Otherwise, get the new marched position
                    float3 pos = rayOrigin + (depth * normRayDir); \
                    \
                    \ // If the position is within the height bounds...
                    if (pos.y > _MinHeight && pos.y < _MaxHeight) \
                    { \
                        \ // Get the cloud density from the noise map
                        float density = noiseMap(pos); \
                        \ // If there is density...
                        if (density > 0.0) \
                        { \
                            \ // Calculate diffuse value at an offset about the sun direction
                            float offsetDensity = noiseMap(pos + (0.3 * _SunDir)); \
                            float densityDifference = density - offsetDensity; \
                            float diffuse = clamp(densityDifference / 0.6, 0.0, 1.0); \
                            \ // Integrate this info into the accumulated cloud color
                            cloudColor = integrate(cloudColor, diffuse, density, bgColor, depth); \
                        } \
                    } \
                    \
                    \ // March along the view direction
                    depth += max(0.1, 0.02 * depth) * _StepScale; \ 
                } \
            }

            // This macro function processes noise values.
            // It fades the noise near the max height bounds to prevent a strong cutoff.
            // N: noise to process
            // P: point
            #define NOISEPROC(N, P)     1.75 * N * saturate(min((_MaxHeight - P.y), (P.y - _MinHeight)) / _FadeDist)

            // Below are some noisemap functions.
            // We are layering all of them to create detailed noise.
            float noiseMap5(float3 q)
            {
                float3 p = q;
                float f;
                f = 0.5 * noiseFromImage(q);
                q = q * 2;
                f += 0.25 * noiseFromImage(q);
                q = q * 3;
                f += 0.125 * noiseFromImage(q);
                q = q * 4;
                f += 0.06250 * noiseFromImage(q);
                q = q * 5;
                f += 0.03125 * noiseFromImage(q);
                q = q * 6;
                f += 0.015625 * noiseFromImage(q);
                return NOISEPROC(f, p);
            } 
            
            float noiseMap4(float3 q)
            {
                float3 p = q;
                float f;
                f = 0.5 * noiseFromImage(q);
                q = q * 2;
                f += 0.25 * noiseFromImage(q);
                q = q * 3;
                f += 0.125 * noiseFromImage(q);
                q = q * 4;
                f += 0.06250 * noiseFromImage(q);
                q = q * 5;
                f += 0.03125 * noiseFromImage(q);
                return NOISEPROC(f, p);
            } 
            
            float noiseMap3(float3 q)
            {
                float3 p = q;
                float f;
                f = 0.5 * noiseFromImage(q);
                q = q * 2;
                f += 0.25 * noiseFromImage(q);
                q = q * 3;
                f += 0.125 * noiseFromImage(q);
                q = q * 4;
                f += 0.06250 * noiseFromImage(q);
                return NOISEPROC(f, p);
            } 
            
            float noiseMap2(float3 q)
            {
                float3 p = q;
                float f;
                f = 0.5 * noiseFromImage(q);
                q = q * 2;
                f += 0.25 * noiseFromImage(q);
                q = q * 3;
                f += 0.125 * noiseFromImage(q);
                return NOISEPROC(f, p);
            } 
            
            float noiseMap1(float3 q)
            {
                float3 p = q;
                float f;
                f = 0.5 * noiseFromImage(q);
                q = q * 2;
                f += 0.25 * noiseFromImage(q);
                return NOISEPROC(f, p);
            }        

            // This is our fragment shader function.
            // It runs on every pixel of the screen and outputs its color.
            fixed4 frag (v2f i) : SV_Target
            {
                // How deep should we march?

                // Get the depth from the view direction ray
                float maxDepth1 = length(i.viewDir);       

                // Get the depth from the camera depth buffer
                float maxDepth2 = tex2D(_CameraDepthTexture, i.scrPos.xy / i.scrPos.w).r;
                maxDepth2 = LinearEyeDepth(maxDepth2);

                // Use the minimum of the two depths
                float maxDepth = min(maxDepth1, maxDepth2);

                fixed4 bgColor = fixed4(1, 1, 1, 0);    

                // Cloud color gets accumulated here
                fixed4 cloudColor = fixed4(0, 0, 0, 0);     

                // Raymarch to get the cloud color value
                float3 normViewDir = normalize(i.viewDir); // Normalize the view direction for marching
                MARCH( noiseMap1, _WorldSpaceCameraPos, normViewDir, maxDepth, bgColor, cloudColor );
                MARCH( noiseMap2, _WorldSpaceCameraPos, normViewDir, maxDepth*2, bgColor, cloudColor );
                MARCH( noiseMap3, _WorldSpaceCameraPos, normViewDir, maxDepth*3, bgColor, cloudColor );
                MARCH( noiseMap4, _WorldSpaceCameraPos, normViewDir, maxDepth*4, bgColor, cloudColor );
                MARCH( noiseMap5, _WorldSpaceCameraPos, normViewDir, maxDepth*5, bgColor, cloudColor );

                // Ensure the color values are in range
                clamp(cloudColor, 0.0, 1.0); 

                // Blend color while maintaining alpha
                cloudColor.rgb = 
                    cloudColor.rgb
                    + bgColor * (1.0 - cloudColor.a);   
                    
                return cloudColor;
            }
            ENDCG
        }
    }
}
