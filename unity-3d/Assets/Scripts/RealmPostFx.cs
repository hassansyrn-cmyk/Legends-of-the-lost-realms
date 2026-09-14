using UnityEngine;
namespace LostRealms {
 // Lightweight BIRP post layer: colour grade (tint/saturation/contrast),
 // vignette and a cheap 4-tap bright bloom. Replaces nothing — it just wraps
 // the camera output, so every existing emissive prop, spell and gate glow
 // reads brighter. Toggleable from the Sanctuary (Save.postFx).
 public sealed class RealmPostFx:MonoBehaviour {
  Material mat;Color tint=Color.white;float bloom=.55f;
  void OnEnable(){if(!mat){var shader=Resources.Load<Shader>("Shaders/RealmPost");if(shader)mat=new Material(shader);}}
  public void Configure(Color t,float bloomAmount=.55f){tint=t;bloom=bloomAmount;if(mat){mat.SetColor("_Tint",tint);mat.SetFloat("_Bloom",bloom);}}
  public void Kick(float amount){if(mat)mat.SetFloat("_Bloom",bloom+amount);}
  void OnRenderImage(RenderTexture src,RenderTexture dst){
   var g=RealmGame.I;
   if(!mat||g==null||!g.Save.postFx){Graphics.Blit(src,dst);return;}
   mat.SetColor("_Tint",tint);mat.SetFloat("_Bloom",bloom);
   // Below 30% HP the edges wash red with an unscaled-time pulse, so the
   // warning keeps breathing even through HitStop.
   float damage=0f;
   var p=g.Player;
   if(p&&g.Screen==GameScreen.Playing){
    float frac=Mathf.Clamp01((float)p.Health/p.MaxHealth);
    if(frac<.3f)damage=(.3f-frac)/.3f*(.5f+.18f*Mathf.Sin(Time.unscaledTime*5.5f));
   }
   mat.SetFloat("_Damage",damage);
   Graphics.Blit(src,dst,mat);
  }
  void OnDestroy(){if(mat)Destroy(mat);}
 }
}
