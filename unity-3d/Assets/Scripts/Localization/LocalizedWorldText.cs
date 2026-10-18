using UnityEngine;
namespace LostRealms {
 [RequireComponent(typeof(TextMesh))]
 public sealed class LocalizedWorldText:MonoBehaviour {
  TextMesh mesh;Font originalFont;Material originalMaterial;string source,lastVisual;int version=-1;
  public static void Set(TextMesh text,string value){
   var binding=text.GetComponent<LocalizedWorldText>();if(!binding)binding=text.gameObject.AddComponent<LocalizedWorldText>();
   binding.source=value;binding.version=-1;text.text=value;binding.lastVisual=value;
  }
  void LateUpdate(){
   if(!mesh){mesh=GetComponent<TextMesh>();originalFont=mesh.font;originalMaterial=GetComponent<Renderer>().sharedMaterial;}
   if(mesh.text!=lastVisual){source=mesh.text;version=-1;}
   if(version==GameLanguage.Version&&source!=null)return;
   if(source==null)source=mesh.text;
   var logical=GameLanguage.Translate(source);bool rtl=GameLanguage.ContainsArabic(logical);
   mesh.font=rtl?GameLanguage.ArabicFont:originalFont;
   GetComponent<Renderer>().sharedMaterial=rtl?GameLanguage.ArabicFont.material:originalMaterial;
   mesh.text=lastVisual=rtl?GameLanguage.Shape(logical):logical;version=GameLanguage.Version;
  }
 }
}
