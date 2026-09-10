Shader "LostRealms/Weathered" {
 Properties { _Color("Base", Color)=(.2,.4,.3,1) _Detail("Detail",Color)=(.3,.5,.25,1) _MainTex("Realm Terrain",2D)="white"{} _Scale("Scale",Float)=1 _TileScale("Terrain Tile Scale",Float)=.13 }
 SubShader { Tags { "RenderType"="Opaque" } LOD 200
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 fixed4 _Color,_Detail; sampler2D _MainTex; float _Scale,_TileScale;
 struct Input { float3 worldPos; };
 float hash(float3 p){ return frac(sin(dot(p,float3(12.9898,78.233,37.719)))*43758.5453); }
 float noise(float3 p){ float3 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(i),hash(i+float3(1,0,0)),f.x),lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),f.x),lerp(hash(i+float3(0,1,1)),hash(i+1),f.x),f.y),f.z); }
 void surf(Input IN,inout SurfaceOutputStandard o){float3 p=IN.worldPos*_Scale;float n=noise(p*.7)*.55+noise(p*3)*.3+noise(p*13)*.15;float3 weathering=lerp(_Color.rgb,_Detail.rgb,smoothstep(.25,.75,n));float3 tile=tex2D(_MainTex,IN.worldPos.xz*_TileScale).rgb;float vignette=noise(IN.worldPos*.32)*.08;float3 terrain=tile*lerp(_Color.rgb,_Detail.rgb,.28);o.Albedo=lerp(weathering,terrain,.82)+vignette;o.Metallic=0;o.Smoothness=.12;o.Occlusion=.94;}
 ENDCG
 } FallBack "Diffuse"
}
