// BIRP full-screen grade + vignette + cheap 4-tap bloom. Loaded from
// Resources/Shaders so it survives build stripping (Resources are always kept).
Shader "LostRealms/RealmPost" {
 Properties {
  _MainTex ("Texture", 2D) = "white" {}
  _Tint ("Tint", Color) = (1,1,1,1)
  _Sat ("Saturation", Range(0,2)) = 1.06
  _Contrast ("Contrast", Range(0,2)) = 1.06
  _Vignette ("Vignette", Range(0,1)) = 0.34
  _Bloom ("Bloom", Range(0,2)) = 0.55
  _Threshold ("Bloom threshold", Range(0,1)) = 0.7
  _Damage ("Damage vignette", Range(0,1)) = 0
 }
 SubShader {
  Cull Off ZWrite Off ZTest Always
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex; float4 _MainTex_TexelSize;
   fixed4 _Tint; float _Sat,_Contrast,_Vignette,_Bloom,_Threshold,_Damage;
   struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
   v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;return o;}
   float3 bright(float2 uv){
    float3 c=tex2D(_MainTex,uv).rgb;
    return max(0.0,c-_Threshold);
   }
   fixed4 frag(v2f i):SV_Target{
    float2 uv=i.uv;
    float3 col=tex2D(_MainTex,uv).rgb;
    // Cheap bloom: 4 wide taps of the bright pass (one tap per quadrant).
    float2 o=_MainTex_TexelSize.xy*2.2;
    float3 b=bright(uv+float2(o.x,o.y))+bright(uv+float2(-o.x,o.y))
            +bright(uv+float2(o.x,-o.y))+bright(uv+float2(-o.x,-o.y));
    col+=b*0.25*_Bloom;
    float l=dot(col,float3(0.299,0.587,0.114));
    col=lerp(float3(l,l,l),col,_Sat);
    col=(col-0.5)*_Contrast+0.5;
    col*=_Tint.rgb;
    float2 d=uv-0.5;
    col*=1.0-_Vignette*dot(d,d)*2.4;
    // Low-HP damage wash: red edges deepening toward the corners.
    float edge=smoothstep(0.04,0.5,dot(d,d)*2.0);
    col=lerp(col,float3(col.r*1.25+0.22,col.g*0.35,col.b*0.3),saturate(_Damage)*edge);
    return fixed4(saturate(col),1);
   }
   ENDCG
  }
 }
}
