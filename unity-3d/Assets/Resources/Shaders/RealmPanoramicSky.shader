Shader "LostRealms/RealmPanoramicSky" {
 Properties {
  _MainTex ("Panoramic Texture (2:1)", 2D) = "white" {}
  _Tint ("Tint Color", Color) = (1,1,1,1)
  _Exposure ("Exposure", Range(0.1, 3.0)) = 1.0
  _Rotation ("Rotation (Degrees)", Range(0, 360)) = 0.0
 }
 SubShader {
  Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
  Cull Off ZWrite Off Fog { Mode Off }
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"

   sampler2D _MainTex;
   fixed4 _Tint;
   half _Exposure;
   float _Rotation;

   struct appdata_t {
    float4 vertex : POSITION;
   };

   struct v2f {
    float4 pos : SV_POSITION;
    float3 dir : TEXCOORD0;
   };

   v2f vert(appdata_t v) {
    v2f o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.dir = v.vertex.xyz;
    return o;
   }

   fixed4 frag(v2f i) : SV_Target {
    float3 d = normalize(i.dir);
    // Spherical equirectangular projection (360x180)
    float u = atan2(d.x, -d.z) / (2.0 * 3.14159265359) + 0.5 + (_Rotation / 360.0);
    float v = asin(clamp(d.y, -1.0, 1.0)) / 3.14159265359 + 0.5;
    fixed4 col = tex2D(_MainTex, float2(u, v));
    col.rgb *= _Tint.rgb * _Exposure;
    return col;
   }
   ENDCG
  }
 }
}
