Shader "Unlit/VolumetricClouds"
{
    Properties
    {
        _CloudNoiseTex3D ("Cloud Noise Texture (3D)", 3D) = "" {}
        _Density("Density", Range(0, 10)) = 1.0
        _RaymarchSteps("Raymarch Steps", Range(1, 128)) = 64
        _LightColor("Light Color", Color) = (1,1,1,1)
        _NoiseThreshold("Noise Threshold", Range(0, 1)) = 0.4
        _EdgeFadeDistance("Edge Fade Distance", Range(0.01, 0.5)) = 0.1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 localPos : TEXCOORD1;
            };

            sampler3D _CloudNoiseTex3D;
            float _Density;
            int _RaymarchSteps;
            float4 _LightColor;
            float _NoiseThreshold;
            float _EdgeFadeDistance;

            // Calculate fade factor based on distance from volume edges
            float CalculateEdgeFade(float3 pos)
            {
                // Calculate distance from each edge as a 0-1 value
                // where 0 is at the boundary and 1 is _EdgeFadeDistance inside
                float3 distFromEdge = 0.5 - abs(pos);
                float3 normalizedDist = saturate(distFromEdge / _EdgeFadeDistance);
                
                // Multiply the fade factors from all three axes
                return normalizedDist.x * normalizedDist.y * normalizedDist.z;
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.localPos = v.vertex.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Transform the view ray direction from world space to local object space
                float3 rayDir = normalize(mul((float3x3)unity_WorldToObject, i.worldPos - _WorldSpaceCameraPos));

                // The starting position for the ray is the point on the cube's surface, in local space
                float3 p = i.localPos;

                float4 finalColor = float4(0, 0, 0, 0);

                // Define a step size. A unit cube's diagonal is ~1.732.
                // This ensures the ray travels across the cube.
                float stepSize = 1.732 / _RaymarchSteps;

                [loop]
                for (int j = 0; j < _RaymarchSteps; j++)
                {
                    // Calculate edge fade factor
                    float edgeFade = CalculateEdgeFade(p);
                    
                    // Remap local position from [-0.5, 0.5] to [0, 1] for texture sampling
                    float3 samplePos = p + 0.5;

                    float noise = tex3D(_CloudNoiseTex3D, samplePos).r;

                    if (noise > _NoiseThreshold)
                    {
                        // Apply edge fade to density
                        float density = (noise - _NoiseThreshold) * _Density / _RaymarchSteps * edgeFade;
                        finalColor.a += density;

                        // Simple lighting
                        finalColor.rgb += _LightColor.rgb * density;

                        // Early exit if fully opaque
                        if(finalColor.a > 0.99)
                           break;
                    }

                    // Move along the ray in local space
                    p += rayDir * stepSize;

                    // Check if we are outside the cube's local bounds [-0.5, 0.5]
                    if (abs(p.x) > 0.5 || abs(p.y) > 0.5 || abs(p.z) > 0.5)
                    {
                        break;
                    }
                }

                finalColor.a = clamp(finalColor.a, 0.0, 1.0);
                // Pre-multiply alpha
                finalColor.rgb *= finalColor.a;

                return finalColor;
            }
            ENDCG
        }
    }
}