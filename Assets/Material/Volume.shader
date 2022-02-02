Shader "Unlit/Volume"
{
    Properties
    {
        _AirPressure ("_AirPressure", 3D) = "white" {}
        _AirTemp ("_AirTemp", 3D) = "white" {}
        _MatTemp ("_MatTemp", 3D) = "white" {}

        _Alpha ("_Alpha", float) = 0.02
        _StepSize ("_StepSize", float) = 0.01

        _AirColorMap ("_AirColorMap", 2D) = "white" {}
        _LowTemp ("_LowTemp", float) = 25
        _HighTemp ("_HighTemp", float) = 80
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend One OneMinusSrcAlpha
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // Maximum amount of raymarching samples
            #define MAX_STEP_COUNT 1028

            // Allowed floating point inaccuracy
            #define EPSILON 0.00001f

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 objectVertex : TEXCOORD0;
                float3 vectorToSurface : TEXCOORD1;
            };

            sampler3D _AirPressure;
            float4 _AirPressure_ST;
            sampler3D _AirTemp;
            float4 _AirTemp_ST;
            sampler3D _MatTemp;
            float4 _MatTemp_ST;

            float _Alpha;
            float _StepSize;

            sampler2D _AirColorMap;
            float4 _AirColorMap_ST;
            float _LowTemp;
            float _HighTemp;

            v2f vert (appdata v)
            {
                v2f o;

                // Vertex in object space this will be the starting point of raymarching
                o.objectVertex = v.vertex;

                // Calculate vector from camera to vertex in world space
                float3 worldVertex = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.vectorToSurface = worldVertex - _WorldSpaceCameraPos;

                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 BlendUnder(float4 color, float4 newColor)
            {
                color.rgb += (1.0 - color.a) * newColor.a * newColor.rgb;
                color.a += (1.0 - color.a) * newColor.a;
                return color;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Start raymarching at the front surface of the object
                float3 rayOrigin = i.objectVertex;

                // Use vector from camera to object surface to get ray direction
                float3 rayDirection = mul(unity_WorldToObject, float4(normalize(i.vectorToSurface), 1));

                float4 color = float4(0, 0, 0, 0);
                float3 samplePosition = rayOrigin;

                // Raymarch through object space
                for (int i = 0; i < MAX_STEP_COUNT; i++)
                {
                    // Accumulate color only within unit cube bounds
                    if(max(abs(samplePosition.x), max(abs(samplePosition.y), abs(samplePosition.z))) < 0.5f + EPSILON)
                    {
                        float4 sampledColor = tex3D(_AirTemp, samplePosition + float3(0.5f, 0.5f, 0.5f));

						float mapped = (sampledColor.r - _LowTemp) / (_HighTemp - _LowTemp);
						float4 mappedColor = tex2D(_AirColorMap, float2(mapped, 0.01));
						sampledColor.rgba = mappedColor.rgba;
                        sampledColor.a *= _Alpha;

                        color = BlendUnder(color, sampledColor);
                        samplePosition += rayDirection * _StepSize;
                    }
                }

                return color;
            }
            ENDCG
        }
    }
}
