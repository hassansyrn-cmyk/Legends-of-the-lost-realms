using UnityEngine;
namespace LostRealms {
 // Lightweight BIRP post layer: colour grade (tint/saturation/contrast),
 // vignette and a cheap 4-tap bright bloom. Replaces nothing — it just wraps
 // the camera output, so every existing emissive prop, spell and gate glow
 // reads brighter. Toggleable from the Sanctuary (Save.postFx).
 public sealed class RealmPostFx:MonoBehaviour {
  Material mat;Color tint=Color.white;float bloom=.3f,kick;
  void OnEnable(){if(!mat){var shader=Resources.Load<Shader>("Shaders/RealmPost");if(shader)mat=new Material(shader);}}
  public void Configure(Color t,float bloomAmount=.55f){tint=t;bloom=bloomAmount;if(mat){mat.SetColor("_Tint",tint);mat.SetFloat("_Bloom",bloom);}}
  public void Kick(float amount){kick=Mathf.Max(kick,Mathf.Clamp(amount,0,.3f));}
  void OnRenderImage(RenderTexture src,RenderTexture dst){
   var g=RealmGame.I;
   if(!mat||g==null||!g.Save.postFx){Graphics.Blit(src,dst);return;}
   kick=Mathf.MoveTowards(kick,0,Time.unscaledDeltaTime*.8f);
   mat.SetColor("_Tint",tint);mat.SetFloat("_Bloom",bloom+kick);
   mat.SetFloat("_Sat",g.Realm==3?.94f:1.02f);
   // Below 30% HP the edges wash red with an unscaled-time pulse, so the
   // warning keeps breathing even through HitStop.
   float damage=0f;
   var p=g.Player;
   if(p&&g.Screen==GameScreen.Playing){
    float frac=Mathf.Clamp01((float)p.Health/p.MaxHealth);
    if(frac<.3f)damage=(.3f-frac)/.3f*(.5f+.18f*Mathf.Sin(Time.unscaledTime*5.5f));
   }
   mat.SetFloat("_Damage",damage);
   // Quarter-resolution bloom keeps the wide blur affordable on mobile.
   int width=Mathf.Max(1,src.width/4),height=Mathf.Max(1,src.height/4);
   var bright=RenderTexture.GetTemporary(width,height,0,src.format);
   var blur=RenderTexture.GetTemporary(width,height,0,src.format);
   bright.filterMode=blur.filterMode=FilterMode.Bilinear;
   try{
    Graphics.Blit(src,bright,mat,1);
    mat.SetVector("_BlurDirection",new Vector4(1,0,0,0));Graphics.Blit(bright,blur,mat,2);
    mat.SetVector("_BlurDirection",new Vector4(0,1,0,0));Graphics.Blit(blur,bright,mat,2);
    mat.SetTexture("_BloomTex",bright);Graphics.Blit(src,dst,mat,0);
   }finally{RenderTexture.ReleaseTemporary(bright);RenderTexture.ReleaseTemporary(blur);}
  }
  void OnDestroy(){if(mat)Destroy(mat);}
 }
}
