Shader "HypeRate/Pulse Sphere"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 0.02, 0.2, 0.1)
        _EmissionColor ("Emission", Color) = (1, 0.02, 0.2, 1)
        _Alpha ("Transparency", Range(0, 1)) = 0.12
        _Density ("Energy Density", Range(1, 20)) = 8
        _Pulse ("Pulse", Range(0, 2)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 200
        Blend SrcAlpha One
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            fixed4 _EmissionColor;
            float _Alpha;
            float _Density;
            float _Pulse;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 objectPosition : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 viewDirection : TEXCOORD2;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.objectPosition = input.vertex.xyz;
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                float3 worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
                output.viewDirection = _WorldSpaceCameraPos.xyz - worldPosition;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float3 normal = normalize(input.worldNormal);
                float3 viewDirection = normalize(input.viewDirection);
                float fresnel = pow(1.0 - saturate(dot(normal, viewDirection)), 2.8);

                float time = _Time.y;
                float flowingBand = sin((input.objectPosition.y * 1.8 + input.objectPosition.x * 0.45 + time * 0.22) * _Density);
                flowingBand = pow(saturate(flowingBand * 0.5 + 0.5), 9.0);
                float crossBand = sin((input.objectPosition.z * 1.6 - input.objectPosition.x * 0.35 - time * 0.15) * (_Density * 0.72));
                crossBand = pow(saturate(crossBand * 0.5 + 0.5), 12.0);

                float energy = fresnel * 1.25 + flowingBand * 0.18 + crossBand * 0.11 + _Pulse * 0.16;
                float3 color = _Color.rgb * 0.18 + _EmissionColor.rgb * energy;
                float alpha = saturate(_Alpha + fresnel * 0.48 + flowingBand * 0.045 + _Pulse * 0.025);
                return fixed4(color, alpha);
            }
            ENDCG
        }
    }

    Fallback "Transparent/VertexLit"
}
