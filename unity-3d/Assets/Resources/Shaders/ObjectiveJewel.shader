Shader "LostRealms/ObjectiveJewel" {
 Properties {
  _MainTex("Original texture",2D)="white"{}
  _Color("Original tint",Color)=(1,1,1,1)
  _BumpMap("Normal",2D)="bump"{}
  _BumpScale("Normal strength",Float)=1
  _Metallic("Metallic",Range(0,1))=.18
  _Glossiness("Smoothness",Range(0,1))=.32
  _JewelGlow("Jewel glow",Color)=(.3,.85,1,1)
  _EmissionColor("Legacy controller compatibility",Color)=(0,0,0,0)
  _GlowStrength("Glow strength",Float)=1.8
 }
 SubShader {
  Tags {"RenderType"="Opaque"}
  LOD 200
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows
  #pragma target 3.0
  #include "UnityStandardUtils.cginc"
  sampler2D _MainTex,_BumpMap;
  fixed4 _Color,_JewelGlow;half _Metallic,_Glossiness,_BumpScale,_GlowStrength;
  struct Input {float2 uv_MainTex;float2 uv_BumpMap;};
  void surf(Input IN,inout SurfaceOutputStandard o){
   fixed4 original=tex2D(_MainTex,IN.uv_MainTex);
   o.Albedo=original.rgb*_Color.rgb;o.Alpha=original.a*_Color.a;
   o.Normal=UnpackScaleNormal(tex2D(_BumpMap,IN.uv_BumpMap),_BumpScale);
   o.Metallic=_Metallic;o.Smoothness=_Glossiness;
   // Cool saturated jewel pixels glow; stone, wood, cloth and gold retain their texture.
   half cool=max(original.b,original.g)-original.r;
   half saturation=max(original.r,max(original.g,original.b))-min(original.r,min(original.g,original.b));
   half jewel=smoothstep(.10,.30,cool)*smoothstep(.18,.45,saturation)*smoothstep(.22,.55,original.b);
   o.Emission=_JewelGlow.rgb*jewel*_GlowStrength;
  }
  ENDCG
 }
 Fallback "Standard"
}
