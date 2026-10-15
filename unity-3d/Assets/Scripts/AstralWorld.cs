using UnityEngine;
namespace LostRealms {
 public static class AstralArt {
  static readonly System.Collections.Generic.Dictionary<string,Material> materials=new System.Collections.Generic.Dictionary<string,Material>();
  public static Material GetMaterial(string asset){
   if(materials.TryGetValue(asset,out var m)&&m)return m;
   var mat=new Material(Shader.Find("Standard")){name="Astral "+asset};
   var tex=Resources.Load<Texture2D>("Astral/Textures/"+asset+"_basecolor");
   var norm=Resources.Load<Texture2D>("Astral/Textures/"+asset+"_normal");
   mat.mainTexture=tex?tex:Resources.Load<Texture2D>("Astral/AstralPalette");
   mat.SetFloat("_Metallic",.25f);
   mat.SetFloat("_Glossiness",.35f);
   if(norm){
    mat.SetTexture("_BumpMap",norm);
    mat.SetFloat("_BumpScale",1f);
    mat.EnableKeyword("_NORMALMAP");
   }
   materials[asset]=mat;
   return mat;
  }
  public static void Label(Transform parent,string value,Vector3 position,Color color){var go=new GameObject(value+" label");go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(90,0,0);var text=go.AddComponent<TextMesh>();text.text=value;text.anchor=TextAnchor.MiddleCenter;text.fontSize=48;text.characterSize=.16f;text.color=color;text.font=Resources.Load<Font>("UI/Fonts/Cinzel-Bold");if(text.font)go.GetComponent<Renderer>().sharedMaterial=text.font.material;}
  public static GameObject Place(string asset,Transform parent,Vector3 pos,Vector3 scale,bool solid=false){
   var prefab=Resources.Load<GameObject>("Astral/"+asset);if(!prefab)throw new System.Exception("Missing Astral asset "+asset);
   var root=new GameObject(asset=="Arch"||asset=="Orrery"?"Prop Astral "+asset:asset);root.transform.SetParent(parent,false);root.transform.localPosition=pos;root.transform.localScale=scale;
   Object.Instantiate(prefab,root.transform,false);
   var mat=GetMaterial(asset);
   foreach(var r in root.GetComponentsInChildren<Renderer>())r.sharedMaterial=mat;
   if(solid)foreach(var mesh in root.GetComponentsInChildren<MeshFilter>()){var c=mesh.gameObject.AddComponent<MeshCollider>();c.sharedMesh=mesh.sharedMesh;}
   return root;
  }
 }
 public partial class RealmWorld {
  static readonly float[][] AstralX={new float[]{0,0,-3,-6,-3,0,4,7,4,0,-3,0},new float[]{0,2,5,5,0,-5,-5,0,5,5,0,-5,-3,0},new float[]{0,0,-4,-7,-4,0,4,7,4,0,-4,-7,-4,0,0},new float[]{0,-3,-6,-3,0,3,6,3,0,-3,-6,-3,0,3,3,0},new float[]{0,0,4,4,0,-4,-4,0,0,0}};
  static readonly int[][] AstralHubs={new[]{3,8},new[]{3,6,10},new[]{3,7,11},new[]{3,6,10,13},new[]{2,5}};
  public void SpawnAstralGuard(Vector3 pos,int kind,Vector3 center,float size){SpawnEnemy(pos,kind,false,center,size,size);}
  public void BuildAstral(int stage){BuildExploration(stage,true);}
 }
 public sealed class AstralFracture:MonoBehaviour {
  public bool Warning=>Mathf.Repeat(RealmGame.I.Elapsed+transform.position.z*.03f,7)>=4.5f;
  float firedCycle=-1;GameObject tell;
  void Update(){var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing)return;float t=g.Elapsed+transform.position.z*.03f;int cycle=Mathf.FloorToInt(t/7);float phase=Mathf.Repeat(t,7);
   if(phase>=4.5f&&!tell&&firedCycle!=cycle){tell=CombatTelegraph.Create(transform.position,2.1f,1.5f,g.World.transform);g.TrapSound("astral_pulse",transform.position);}
   if(phase>=6&&firedCycle!=cycle){firedCycle=cycle;if(tell)Destroy(tell);tell=null;Vfx.Play("ga_vfx_Implosion_01",transform.position+Vector3.up*.15f,Quaternion.identity,.7f);
    if(Vector3.Distance(g.Player.transform.position,transform.position)<2.1f)g.Player.Damage(1,transform.position);
    foreach(var e in g.Enemies)if(e&&!e.Boss&&e.Health>0&&Vector3.Distance(e.transform.position,transform.position)<2.1f)e.PushBack((e.transform.position-transform.position).normalized,.9f,.22f);
   }
  }
 }
}
