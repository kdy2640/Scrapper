Shader "Custom/Plane"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows
        #pragma surface surf Standard fullforwardshadows vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos; 
        };


        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
        }

    void surf (Input IN, inout SurfaceOutputStandard o)
    {
        fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

        // 높이에 따라 색상 다르게 설정 (예: y < 0이면 물 느낌)
        if (IN.worldPos.y < 5.0)
        {
            o.Albedo = float3(0.80, 0.46, 0.28); // 흙 색
            o.Metallic = 0.99;
            o.Smoothness = 0;
        }
        else
        {
            o.Albedo = float3(0.72, 1.0, 0.58); // 풀 색
            o.Metallic = 0.99;
            o.Smoothness = 0; 
        }

        o.Alpha = c.a;
    }

        ENDCG
    }
    FallBack "Diffuse"
}
