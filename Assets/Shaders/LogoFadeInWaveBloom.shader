Shader "UI/LogoFadeInWaveBloom"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Progress ("Fade Progress", Range(0,1)) = 0.0
        _WaveAmplitude ("Wave Amplitude", Range(0,0.1)) = 0.05
        _WaveFrequency ("Wave Frequency", Range(0,20)) = 10
        _WaveSpeed ("Wave Speed", Range(0,5)) = 1
        _BloomColor ("Bloom Color", Color) = (1,1,1,1)
        _BloomIntensity ("Bloom Intensity", Range(0,5)) = 1.0
    }
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "Canvas"="true" 
            "PreviewType"="Plane" 
        }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Progress;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveSpeed;
            float4 _BloomColor;
            float _BloomIntensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calcula una onda basada en la coordenada Y, la frecuencia y el tiempo.
                float wave = sin(i.uv.y * _WaveFrequency + _Time.y * _WaveSpeed);
                // La distorsión se atenua conforme _Progress se acerca a 1 (final de la animación).
                float distortion = (1.0 - _Progress) * _WaveAmplitude * wave;
                float2 distortedUV = i.uv + float2(distortion, 0);

                // Obtiene el color de la textura usando las UV distorsionadas.
                fixed4 col = tex2D(_MainTex, distortedUV);

                // Efecto de fade in: el alfa se multiplica por _Progress.
                col.a *= _Progress;

                // Bloom: se añade brillo extra basado en la onda, atenuado conforme _Progress aumenta.
                float bloomFactor = (1.0 - _Progress) * _BloomIntensity * (wave * 0.5 + 0.5);
                col.rgb += _BloomColor.rgb * bloomFactor;

                return col;
            }
            ENDCG
        }
    }
}
