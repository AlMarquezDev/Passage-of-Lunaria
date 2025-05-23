Shader "Unlit/DeathShader"
{
    Properties
    {
        // Propiedades originales y a�adidas
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        [MainColor]   _Color ("Tint Color", Color) = (1,1,1,1) // Tinte base del sprite

        [Header(Dissolve Properties)]
        _DissolveTex ("Dissolve Noise Texture", 2D) = "gray" {} // Textura de ruido (escala de grises)
        _DissolveAmount ("Dissolve Amount", Range(0.0, 1.0)) = 0.0 // Controla el progreso (0=visible, 1=disuelto)
        _EdgeColor ("Edge Color", Color) = (1,1,0,1) // Color del borde
        _EdgeWidth ("Edge Width", Range(0.0, 0.05)) = 0.01 // Grosor relativo del borde

        [Header(Impulse Properties)]
        _ImpulseDirection ("Impulse Direction (XYZ)", Vector) = (-1, 0.5, 0, 0) // Direcci�n del empuje
        _ImpulseStrength ("Impulse Strength", Float) = 1.0 // Fuerza del empuje
    }
    SubShader
    {
        // --- Estados de Renderizado para Transparencia con Recorte ---
        Tags
        {
            "Queue"="Transparent"           // Renderizar despu�s de opacos
            "RenderType"="TransparentCutout" // Indica que usa recorte alfa
            "IgnoreProjector"="True"
            "PreviewType"="Plane"           // �til para previsualizar en assets
            "CanUseSpriteAtlas"="True"      // Para Sprites
        }
        LOD 100

        Cull Off // Desactivar culling para sprites 2D
        Lighting Off // Shader Unlit
        ZWrite Off // Desactivar escritura en Z para transparencia correcta
        Blend SrcAlpha OneMinusSrcAlpha // Mezcla alfa est�ndar

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog // Mantener soporte de niebla si se usa

            #include "UnityCG.cginc" // Incluir funciones est�ndar de Unity

            // --- Estructuras de Datos ---
            struct appdata
            {
                float4 vertex : POSITION;      // Posici�n del v�rtice (espacio de objeto)
                float2 uv : TEXCOORD0;       // Coordenadas UV principales
                float4 color : COLOR;        // Color de v�rtice (importante para SpriteRenderer)
            };

            struct v2f // Datos pasados del vertex al fragment
            {
                float2 uv : TEXCOORD0;       // UVs interpoladas
                float4 vertex : SV_POSITION; // Posici�n final (espacio de clip)
                float4 color : COLOR;        // Color de v�rtice interpolado
                UNITY_FOG_COORDS(1)          // Coordenadas de niebla
            };

            // --- Variables Uniformes (propiedades del shader) ---
            sampler2D _MainTex;
            float4 _MainTex_ST; // Tiling/Offset para _MainTex
            sampler2D _DissolveTex;
            float4 _DissolveTex_ST; // Tiling/Offset para _DissolveTex

            fixed4 _Color;
            fixed4 _EdgeColor;
            half _DissolveAmount;
            half _EdgeWidth;
            float3 _ImpulseDirection;
            float _ImpulseStrength;

            // --- Vertex Shader ---
            v2f vert (appdata v)
            {
                v2f o;

                float3 positionOS = v.vertex.xyz; // Posici�n original en espacio de objeto

                // --- C�lculo del Impulso ---
                if (_ImpulseStrength > 0)
                {
                    // Factor de fuerza (ej. m�s fuerte al inicio, disminuye cuadr�ticamente)
                    // Puedes experimentar con otras funciones (1.0 - _DissolveAmount, smoothstep, etc.)
                    float impulseFactor = pow(1.0 - saturate(_DissolveAmount), 2.0); // saturate asegura 0-1

                    // Calcular desplazamiento
                    // Normalizar direcci�n por si acaso no est� normalizada en el Inspector
                    float3 displacement = normalize(_ImpulseDirection) * _ImpulseStrength * impulseFactor;

                    // Aplicar desplazamiento
                    positionOS += displacement;
                }
                // --- Fin Impulso ---

                // Transformar a espacio de clip
                o.vertex = UnityObjectToClipPos(float4(positionOS, 1.0));

                // Pasar UVs (aplicando tiling/offset)
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                // Pasar color de v�rtice
                o.color = v.color;

                // Pasar coordenadas de niebla
                UNITY_TRANSFER_FOG(o,o.vertex);

                return o;
            }

            // --- Fragment Shader ---
            fixed4 frag (v2f i) : SV_Target
            {
                // Muestrear textura principal (Sprite)
                fixed4 mainTexColor = tex2D(_MainTex, i.uv);

                // Aplicar tinte base y color de v�rtice
                fixed4 baseColor = mainTexColor * _Color * i.color;

                // Muestrear textura de ruido
                // Usar las mismas UVs o unas diferentes si quieres un patr�n de ruido independiente del sprite
                half dissolveValue = tex2D(_DissolveTex, i.uv).r; // Usar un canal (ej. Rojo)

                // Calcular umbral de disoluci�n
                // A�adir un peque�o offset basado en EdgeWidth para que el borde se calcule correctamente
                half dissolveThreshold = _DissolveAmount;

                // --- L�gica de Recorte (Clip) ---
                // Descarta el p�xel si el valor de ruido es menor que el umbral de disoluci�n
                clip(dissolveValue - dissolveThreshold);
                // Si el p�xel sobrevive a clip(), significa que dissolveValue >= dissolveThreshold

                // --- L�gica del Borde (Opcional) ---
                fixed3 finalColor = baseColor.rgb; // Empezar con el color base
                if (_EdgeWidth > 0.0)
                {
                     // Calcular si estamos dentro del rango del borde
                     // El borde est� entre dissolveThreshold y dissolveThreshold + edgeWidth
                     half edgeCheck = step(dissolveThreshold + _EdgeWidth, dissolveValue); // 1 si est� por encima del borde, 0 si est� dentro o por debajo
                     half isEdge = 1.0 - edgeCheck; // Ser� 1 si estamos EN el borde (o por debajo, pero clip() ya elimin� los de debajo)

                     // Interpolar hacia el color del borde si isEdge es 1
                     // Usamos saturate para asegurar que no haya valores negativos si EdgeWidth es muy peque�o
                     finalColor = lerp(finalColor, _EdgeColor.rgb, saturate(isEdge));
                }

                // Aplicar niebla al color final
                UNITY_APPLY_FOG(i.fogCoord, finalColor);

                // Devolver el color final con el alfa original del sprite (modificado por el color de v�rtice)
                // El recorte se hace con clip(), no modificando el alfa aqu�.
                return fixed4(finalColor, baseColor.a);
            }
            ENDCG
        }
    }
}