Shader "UI/TintedRippleBackground"
{
    Properties
    {
        _MainTex ("Background Texture", 2D) = "white" {}
        _Color1 ("Tint Color 1", Color) = (0, 0, 1, 1)
        _Color2 ("Tint Color 2", Color) = (0.5, 0.5, 1, 1)
        _Angle ("Gradient Angle", Range(0, 360)) = 0
        _RippleSpeed ("Ripple Speed", Float) = 1.0
        _RippleFrequency ("Ripple Frequency", Float) = 10.0
        _RippleAmplitude ("Ripple Amplitude", Float) = 0.01
        _TintIntensity ("Tint Strength", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color1;
            float4 _Color2;
            float _Angle;
            float _RippleSpeed;
            float _RippleFrequency;
            float _RippleAmplitude;
            float _TintIntensity;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // Calculate ripple distortion
                float2 center = i.uv - 0.5;
                float distance = length(center);
                float ripple = sin(distance * _RippleFrequency + _Time.y * _RippleSpeed) * _RippleAmplitude;
                float2 displacedUV = i.uv + center * ripple;

                // Sample background texture
                fixed4 bgColor = tex2D(_MainTex, displacedUV);

                // Calculate gradient
                float angleRad = radians(_Angle);
                float2 dir = float2(cos(angleRad), sin(angleRad));
                float t = dot(displacedUV - 0.5, dir) + 0.5;
                t = saturate(t);
                t = smoothstep(0.0, 1.0, t);
                fixed4 gradient = lerp(_Color1, _Color2, t);

                // Blend between original texture and tinted version
                fixed4 tintedColor = lerp(bgColor, bgColor * gradient, _TintIntensity);

                return tintedColor;
            }
            ENDCG
        }
    }
}