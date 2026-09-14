using UnityEditor;
using UnityEngine;

// Rebuilds the four degenerate showcase families (Flyer/Bomber/Summoner/Elite:
// their decimated FBX collapsed to 2cm point-clouds, see Validation/all-roles)
// as hand-built primitive prefabs under Resources/Characters. Same pattern as
// the original game models: a static multi-part body driven at runtime by the
// Shared root-motion clips + weakness aura, no rig needed.
// Menu: Lost Realms/Phase 2/Rebuild Primitive Enemies. Headless-safe.
public static class PrimitiveEnemies {
 struct Part { public PrimitiveType type; public Vector3 pos,scale; public Color color; public float emission; public Vector3 rot;
  public Part(PrimitiveType t,Vector3 p,Vector3 s,Color c,float e,Vector3 r){type=t;pos=p;scale=s;color=c;emission=e;rot=r;} }
 static Material Mat(string name,Color c,float emission){
  string path="Assets/Resources/Characters/Materials/"+name+".mat";
  var m=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!m){
   m=new Material(Shader.Find("Standard")){name=name};
   AssetDatabase.CreateAsset(m,path);
  }
  m.color=c;m.SetFloat("_Metallic",.15f);m.SetFloat("_Glossiness",.35f);
  if(emission>0f){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*emission);}
  else{m.DisableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);}
  EditorUtility.SetDirty(m);return m;
 }
 static void Build(string role,Part[] parts){
  var root=new GameObject(role);
  foreach(var p in parts){
   var go=GameObject.CreatePrimitive(p.type);
   go.name=p.type.ToString();
   go.transform.SetParent(root.transform,false);
   go.transform.localPosition=p.pos;go.transform.localScale=p.scale;
   go.transform.localRotation=Quaternion.Euler(p.rot);
   var col=go.GetComponent<Collider>();if(col)Object.DestroyImmediate(col);
   var r=go.GetComponent<Renderer>();
   r.sharedMaterial=Mat(role+"_"+go.name+"_"+ColorUtility.ToHtmlStringRGB(p.color),p.color,p.emission);
   r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;r.receiveShadows=true;
  }
  string path="Assets/Resources/Characters/"+role+".prefab";
  PrefabUtility.SaveAsPrefabAsset(root,path);
  Object.DestroyImmediate(root);
 }
 [MenuItem("Lost Realms/Phase 2/Rebuild Primitive Enemies")]
 public static void Rebuild(){
  var dir="Assets/Resources/Characters/Materials";
  if(!AssetDatabase.IsValidFolder(dir))AssetDatabase.CreateFolder("Assets/Resources/Characters","Materials");
  // Flyer: hovering energy orb (kind 11 floats +2.3m and bobs; keep mass centered ~1.1).
  var arcane=new Color(.62f,.34f,1f);var deep=new Color(.16f,.12f,.24f);
  Build("Flyer",new[]{
   new Part(PrimitiveType.Sphere,new Vector3(0,1.1f,0),new Vector3(.64f,.64f,.64f),arcane,1.8f,Vector3.zero),
   new Part(PrimitiveType.Sphere,new Vector3(0,1.1f,0),new Vector3(.9f,.5f,.9f),deep,.25f,Vector3.zero),
   new Part(PrimitiveType.Cube,new Vector3(.62f,1.1f,0),new Vector3(.16f,.16f,.16f),arcane,1.4f,new Vector3(0,0,20)),
   new Part(PrimitiveType.Cube,new Vector3(-.62f,1.1f,0),new Vector3(.16f,.16f,.16f),arcane,1.4f,new Vector3(0,0,-20)),
   new Part(PrimitiveType.Cube,new Vector3(0,1.1f,.62f),new Vector3(.16f,.16f,.16f),arcane,1.4f,new Vector3(20,0,0)),
   new Part(PrimitiveType.Cube,new Vector3(0,1.1f,-.62f),new Vector3(.16f,.16f,.16f),arcane,1.4f,new Vector3(-20,0,0)),
   new Part(PrimitiveType.Cylinder,new Vector3(0,.42f,0),new Vector3(.12f,.55f,.12f),arcane,1.2f,Vector3.zero),
  });
  // Bomber: squat lava rock golem that waddles in and detonates.
  var basalt=new Color(.19f,.15f,.15f);var lava=new Color(1f,.36f,.1f);
  Build("Bomber",new[]{
   new Part(PrimitiveType.Cube,new Vector3(-.3f,.3f,0),new Vector3(.34f,.6f,.4f),basalt,0f,Vector3.zero),
   new Part(PrimitiveType.Cube,new Vector3(.3f,.3f,0),new Vector3(.34f,.6f,.4f),basalt,0f,Vector3.zero),
   new Part(PrimitiveType.Cube,new Vector3(0,.95f,0),new Vector3(.95f,.75f,.7f),basalt,0f,Vector3.zero),
   new Part(PrimitiveType.Cube,new Vector3(0,.95f,.36f),new Vector3(.5f,.4f,.06f),lava,1.6f,Vector3.zero),
   new Part(PrimitiveType.Cube,new Vector3(-.62f,.9f,0),new Vector3(.28f,.7f,.32f),basalt,0f,new Vector3(0,0,8)),
   new Part(PrimitiveType.Cube,new Vector3(.62f,.9f,0),new Vector3(.28f,.7f,.32f),basalt,0f,new Vector3(0,0,-8)),
   new Part(PrimitiveType.Cube,new Vector3(0,1.5f,0),new Vector3(.5f,.38f,.45f),basalt,0f,Vector3.zero),
   new Part(PrimitiveType.Cube,new Vector3(-.13f,1.52f,.23f),new Vector3(.11f,.09f,.05f),lava,2f,Vector3.zero),
   new Part(PrimitiveType.Cube,new Vector3(.13f,1.52f,.23f),new Vector3(.11f,.09f,.05f),lava,2f,Vector3.zero),
  });
  // Summoner: hooded wizard with a glowing staff.
  var robe=new Color(.3f,.18f,.48f);var pale=new Color(.82f,.72f,.6f);
  Build("Summoner",new[]{
   new Part(PrimitiveType.Cylinder,new Vector3(0,.55f,0),new Vector3(.62f,1.1f,.62f),robe,0f,Vector3.zero),
   new Part(PrimitiveType.Sphere,new Vector3(0,1.28f,0),new Vector3(.34f,.36f,.34f),pale,0f,Vector3.zero),
   new Part(PrimitiveType.Cylinder,new Vector3(0,1.52f,0),new Vector3(.5f,.06f,.5f),robe,.2f,Vector3.zero),
   new Part(PrimitiveType.Cylinder,new Vector3(0,1.78f,0),new Vector3(.22f,.5f,.22f),robe,.2f,Vector3.zero),
   new Part(PrimitiveType.Sphere,new Vector3(0,2.02f,0),new Vector3(.07f,.07f,.07f),robe,.4f,Vector3.zero),
   new Part(PrimitiveType.Cylinder,new Vector3(.48f,.8f,0),new Vector3(.07f,1.6f,.07f),new Color(.24f,.17f,.12f),0f,Vector3.zero),
   new Part(PrimitiveType.Sphere,new Vector3(.48f,1.72f,0),new Vector3(.22f,.22f,.22f),new Color(.75f,.4f,1f),1.8f,Vector3.zero),
  });
  // Elite: tall crystal knight mini-boss (kind 14 normalizes to 2.2m).
  var steel=new Color(.32f,.35f,.42f);var cyan=new Color(.35f,.85f,1f);
  Build("Elite",new[]{
   new Part(PrimitiveType.Cube,new Vector3(-.2f,.35f,0),new Vector3(.26f,.7f,.3f),steel,0f,Vector3.zero),
   new Part(PrimitiveType.Cube,new Vector3(.2f,.35f,0),new Vector3(.26f,.7f,.3f),steel,0f,Vector3.zero),
   new Part(PrimitiveType.Cube,new Vector3(0,1.15f,0),new Vector3(.72f,.95f,.5f),steel,0f,Vector3.zero),
   new Part(PrimitiveType.Cube,new Vector3(0,1.2f,.26f),new Vector3(.4f,.55f,.05f),cyan,1.4f,Vector3.zero),
   new Part(PrimitiveType.Cube,new Vector3(-.52f,1.45f,0),new Vector3(.34f,.3f,.42f),steel,0f,Vector3.zero),
   new Part(PrimitiveType.Cube,new Vector3(.52f,1.45f,0),new Vector3(.34f,.3f,.42f),steel,0f,Vector3.zero),
   new Part(PrimitiveType.Cube,new Vector3(0,1.85f,0),new Vector3(.4f,.42f,.42f),steel,0f,Vector3.zero),
   new Part(PrimitiveType.Cube,new Vector3(0,1.86f,.21f),new Vector3(.28f,.1f,.05f),cyan,2f,Vector3.zero),
   new Part(PrimitiveType.Cube,new Vector3(0,1.15f,-.42f),new Vector3(.22f,.7f,.22f),cyan,1.2f,new Vector3(45,45,0)),
   new Part(PrimitiveType.Cube,new Vector3(.62f,1.1f,.1f),new Vector3(.12f,1.1f,.06f),new Color(.8f,.87f,.95f),.3f,Vector3.zero),
  });
  AssetDatabase.SaveAssets();
  Debug.Log("PRIMITIVE_ENEMIES_PASSED");
 }
}
