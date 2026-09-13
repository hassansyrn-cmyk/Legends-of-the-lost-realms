Shader "LostRealms/RealmSkyBox" {
 Properties {
  _TopColor ("Top color", Color) = (0.13,0.38,0.52,1)
  _HorizonColor ("Horizon color", Color) = (0.55,0.72,0.66,1)
  _GroundColor ("Below-horizon color", Color) = (0.1,0.16,0.16,1)
  _SunColor ("Sun color", Color) = (1,0.93,0.75,1)
  _SunDir ("Sun direction", Vector) = (0.3,0.5,0.6,0)
  _SunSize ("Sun disc size", Range(0.998,0.9999)) = 0.9993
  _StarAmount ("Stars", Range(0,1)) = 0.25
 }
 SubShader {
  Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
  Cull Off ZWrite Off Fog { Mode Off }
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   fixed3 _TopColor,_HorizonColor,_GroundColor,_SunColor;
   float3 _SunDir;float _SunSize,_StarAmount;
   struct v2f{float4 pos:SV_POSITION;float3 dir:TEXCOORD0;};
   v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.dir=v.vertex.xyz;return o;}
   float hash13(float3 p){p=frac(p*0.1031);p+=dot(p,p.zyx+31.32);return frac((p.x+p.y)*p.z);}
   fixed4 frag(v2f i):SV_Target{
    float3 d=normalize(i.dir);
    float h=d.y;
    fixed3 col=h>=0?lerp(_HorizonColor,_TopColor,pow(saturate(h*1.4),0.62)):lerp(_HorizonColor,_GroundColor,pow(saturate(-h*2.2),0.7));
    col=lerp(col,_HorizonColor,pow(1-abs(h),9)*0.85);
    float s=max(dot(d,normalize(_SunDir)),0);
    col+=_SunColor*(smoothstep(_SunSize,0.99996,s)*1.6+pow(s,180)*0.5+pow(s,8)*0.12);
    float3 cell=floor(d*230);
    float star=step(0.9986,hash13(cell))*smoothstep(0.08,0.5,h)*_StarAmount;
    col+=star*(0.5+0.5*hash13(cell+7.7));
    return fixed4(col,1);
   }
   ENDCG
  }
 }
}
