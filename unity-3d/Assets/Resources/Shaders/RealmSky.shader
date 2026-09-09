Shader "LostRealms/RealmSky" {
 Properties { _Zenith("Zenith",Color)=(.08,.22,.32,1) _Horizon("Horizon",Color)=(.6,.78,.72,1) }
 SubShader { Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" } Cull Off ZWrite Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 fixed4 _Zenith,_Horizon;
 struct appdata { float4 vertex:POSITION; }; struct v2f { float4 pos:SV_POSITION; float3 dir:TEXCOORD0; };
 v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.dir=v.vertex.xyz;return o;}
 fixed4 frag(v2f i):SV_Target {float3 d=normalize(i.dir);float t=pow(saturate(d.y*.7+.16),.6);float3 c=lerp(_Horizon.rgb,_Zenith.rgb,t);float sun=pow(saturate(dot(d,normalize(float3(-.45,.52,.7)))),480);c+=float3(1,.8,.45)*sun*.7;return float4(c,1);}
 ENDCG }
 }
}
