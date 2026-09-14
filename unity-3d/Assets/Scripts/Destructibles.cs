using System.Collections.Generic;
using UnityEngine;
namespace LostRealms {
 // Breakable crates/barrels: hit them with the blade and they burst into
 // physics shards, sometimes dropping a coin. Registered in a static list so
 // Hero.Attack can test reach without scene-wide searches.
 public sealed class BreakableCrate:MonoBehaviour {
  public static readonly List<BreakableCrate> All=new List<BreakableCrate>();
  Material shardMat;Color shardColor;
  public static void Place(Transform island,Vector3 localPos,Color color,bool barrel=false){
   var go=new GameObject(barrel?"Breakable barrel":"Breakable crate");
   go.transform.SetParent(island,false);go.transform.localPosition=localPos;
   var b=go.AddComponent<BreakableCrate>();b.shardColor=color;b.shardMat=Art.Material(color);
   var shape=Art.Shape("Crate body",barrel?PrimitiveType.Cylinder:PrimitiveType.Cube,Vector3.up*.35f,barrel?new Vector3(.34f,.35f,.34f):new Vector3(.66f,.66f,.66f),color,go.transform);
   Art.Shape("Crate band",barrel?PrimitiveType.Cylinder:PrimitiveType.Cube,Vector3.up*.35f,barrel?new Vector3(.38f,.09f,.38f):new Vector3(.72f,.12f,.72f),new Color(.3f,.22f,.12f),go.transform);
   var col=go.AddComponent<BoxCollider>();col.center=Vector3.up*.35f;col.size=barrel?new Vector3(.7f,.72f,.7f):Vector3.one*.7f;
   All.Add(b);
  }
  void OnDestroy(){All.Remove(this);}
  public void Break(){
   var g=RealmGame.I;if(g==null)return;
   for(int i=0;i<7;i++){
    var f=GameObject.CreatePrimitive(PrimitiveType.Cube);f.name="Shard";
    f.transform.position=transform.position+Vector3.up*.4f+Random.insideUnitSphere*.22f;
    f.transform.localScale=Vector3.one*Random.Range(.12f,.24f);
    f.transform.rotation=Random.rotation;
    var r=f.GetComponent<Renderer>();r.sharedMaterial=shardMat;
    var rbh=f.AddComponent<Rigidbody>();rbh.mass=.35f;
    rbh.AddForce(Vector3.up*Random.Range(1.5f,3f)+Random.insideUnitSphere*2.2f,ForceMode.VelocityChange);
    rbh.AddTorque(Random.insideUnitSphere*4f,ForceMode.VelocityChange);
    Destroy(f,2.4f);
   }
   if(Random.value<.55f)g.Collect(false);
   g.Sound("impact");
   HitSpark.Burst(transform.position+Vector3.up*.4f,Vector3.up,new Color(1f,.82f,.42f),10);
   KenneyPuff.Burst(transform.position+Vector3.up*.3f,shardColor,8,.8f);
   ImpactMarks.Place(transform.position,.7f,new Color(.4f,.34f,.26f));
   Destroy(gameObject);
  }
 }
 // Pushable block: a real rigidbody the CharacterController shoves via
 // Hero.OnControllerColliderHit. Rotation is frozen so it slides like a crate.
 public sealed class PushableBlock:MonoBehaviour {
  public static void Place(Transform island,Vector3 localPos,Color color){
   var go=new GameObject("Pushable block");go.transform.SetParent(island,false);go.transform.localPosition=localPos;
   Art.Shape("Block body",PrimitiveType.Cube,Vector3.up*.4f,new Vector3(.85f,.8f,.85f),color,go.transform);
   Art.Shape("Block rune",PrimitiveType.Cube,Vector3.up*.4f,new Vector3(.9f,.14f,.9f),new Color(.85f,.8f,.55f),go.transform);
   var col=go.AddComponent<BoxCollider>();col.center=Vector3.up*.4f;col.size=new Vector3(.85f,.8f,.85f);
   var rb=go.AddComponent<Rigidbody>();rb.mass=9f;rb.linearDamping=5f;rb.angularDamping=8f;
   rb.constraints=RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;
   go.AddComponent<PushableBlock>();
  }
 }
 // Converted static lanterns into a gentle pendulum.
 public sealed class LanternSwing:MonoBehaviour {
  Transform pivot;float phase;
  public void Bind(Transform p){pivot=p;phase=Random.value*6.283f;}
  void Update(){
   if(!pivot)return;var g=RealmGame.I;if(g&&g.Screen!=GameScreen.Playing)return;
   pivot.localRotation=Quaternion.Euler(0,0,Mathf.Sin(Time.timeSinceLevelLoad*1.5f+phase)*9f);
  }
 }
}
