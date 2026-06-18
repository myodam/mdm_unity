Shader "UI/PageCurl"
{
    Properties
    {
        _MainTex ("Front", 2D) = "white" {}
        _Flip ("Flip Progress", Range(0, 1)) = 0.0
        _Radius ("Curl Radius", Range(0.01, 0.5)) = 0.1
        _Angle ("Angle", float) = 135.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float _Flip;
            float _Radius;
            float _Angle;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float rad = _Angle * 3.14159265 / 180.0;
                float2 dir = float2(cos(rad), sin(rad));
                float2 origin = float2(1.0, 0.0);
                
                float maxDist = 1.5;
                float linePos = maxDist - (_Flip * (maxDist + _Radius * 3.14159265 * 2.0));
                
                float d = dot(uv - origin, dir) - linePos;

                if (d < 0.0) 
                {
                    return tex2D(_MainTex, uv) * i.color;
                }
                else if (d < _Radius * 3.14159265)
                {
                    float angle = d / _Radius;
                    float2 curlUV = uv - dir * (d - sin(angle) * _Radius);
                    
                    if (curlUV.x < 0.0 || curlUV.x > 1.0 || curlUV.y < 0.0 || curlUV.y > 1.0)
                    {
                        return float4(0,0,0,0);
                    }
                    
                    float4 col = tex2D(_MainTex, curlUV);
                    
                    if (angle > 3.14159265 / 2.0) 
                    {
                        col.rgb *= 0.6; 
                    }
                    else
                    {
                        col.rgb *= (0.8 + 0.2 * cos(angle));
                    }
                    col.a *= i.color.a;
                    return col;
                }
                else
                {
                    float shadowDist = d - _Radius * 3.14159265;
                    if (shadowDist < 0.1)
                    {
                        float shadow = smoothstep(0.1, 0.0, shadowDist) * 0.4;
                        return float4(0,0,0, shadow * i.color.a);
                    }
                    return float4(0,0,0,0);
                }
            }
            ENDCG
        }
    }
}
