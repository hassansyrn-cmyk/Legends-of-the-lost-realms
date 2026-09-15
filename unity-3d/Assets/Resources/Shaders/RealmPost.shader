Shader "LostRealms/RealmPost" {
 Properties {
  _MainTex ("Texture", 2D) = "white" {}
  _BloomTex ("Soft bloom", 2D) = "black" {}
  _Tint ("Tint", Color) = (1,1,1,1)
  _Sat ("Saturation", Range(0,2)) = 1.02
  _Contrast ("Contrast", Range(0,2)) = 1.025
  _Vignette ("Vignette", Range(0,1)) = 0.2
  _Bloom ("Bloom", Range(0,2)) = 0.3
  _Threshold ("Bloom threshold", Range(0,1)) = 0.72
  _Damage ("Damage vignette", Range(0,1)) = 0
 }
 SubShader {
  Cull Off ZWrite Off ZTest Always
  CGINCLUDE
  #include "UnityCG.cginc"
  sampler2D _MainTex, _BloomTex;
  float4 _MainTex_TexelSize, _BlurDirection;
  float4 _Tint;
  float _Sat,_Contrast,_Vignette,_Bloom,_Threshold,_Damage;
  ENDCG
  Pass {
   CGPROGRAM
   #pragma vertex vert_img
   #pragma fragment composite
   fixed4 composite(v2f_img i):SV_Target {
    float3 col=tex2D(_MainTex,i.uv).rgb;
    col+=tex2D(_BloomTex,i.uv).rgb*_Bloom;
    float l=dot(col,float3(.299,.587,.114));
    col=lerp(l.xxx,col,_Sat);
    col=(col-.5)*_Contrast+.5;
    col+=.014*(1-saturate(l))*(1-saturate(l));
    col*=_Tint.rgb;
    float2 d=i.uv-.5;
    col*=1-_Vignette*smoothstep(.12,.5,dot(d,d));
    float edge=smoothstep(.04,.5,dot(d,d)*2);
    col=lerp(col,float3(col.r*1.1+.15,col.g*.5,col.b*.45),saturate(_Damage)*edge);
    return fixed4(saturate(col),1);
   }
   ENDCG
  }
  Pass {
   CGPROGRAM
   #pragma vertex vert_img
   #pragma fragment prefilter
   fixed4 prefilter(v2f_img i):SV_Target {
    float2 o=_MainTex_TexelSize.xy;
    float3 c=(tex2D(_MainTex,i.uv+o).rgb+tex2D(_MainTex,i.uv-o).rgb+
              tex2D(_MainTex,i.uv+float2(o.x,-o.y)).rgb+tex2D(_MainTex,i.uv+float2(-o.x,o.y)).rgb)*.25;
    float b=max(c.r,max(c.g,c.b));
    float soft=clamp(b-_Threshold+.15,0,.3);soft=soft*soft/.6;
    float contribution=max(b-_Threshold,soft)/max(b,.0001);
    return fixed4(c*contribution,1);
   }
   ENDCG
  }
  Pass {
   CGPROGRAM
   #pragma vertex vert_img
   #pragma fragment blur
   fixed4 blur(v2f_img i):SV_Target {
    float2 d=_MainTex_TexelSize.xy*_BlurDirection.xy;
    float3 c=tex2D(_MainTex,i.uv).rgb*.227027;
    c+=(tex2D(_MainTex,i.uv+d*1.384615).rgb+tex2D(_MainTex,i.uv-d*1.384615).rgb)*.316216;
    c+=(tex2D(_MainTex,i.uv+d*3.230769).rgb+tex2D(_MainTex,i.uv-d*3.230769).rgb)*.070270;
    return fixed4(c,1);
   }
   ENDCG
  }
 }
}
