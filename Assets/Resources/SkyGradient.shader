Shader "Impulse/SkyGradient" {
 Properties { _Top("Zenith",Color)=(0.035,0.09,0.18,1) _Horizon("Horizon",Color)=(0.32,0.40,0.49,1) }
 SubShader { Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" } Cull Off ZWrite Off
  Pass { CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct appdata {float4 vertex:POSITION;};struct v2f {float4 position:SV_POSITION;float3 direction:TEXCOORD0;};
   float4 _Top,_Horizon;
   v2f vert(appdata v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.direction=v.vertex.xyz;return o;}
   fixed4 frag(v2f i):SV_Target {float h=saturate((normalize(i.direction).y+.35)/1.35);float t=pow(h,.55);return lerp(_Horizon,_Top,t);}
   ENDCG
  }
 }
}
