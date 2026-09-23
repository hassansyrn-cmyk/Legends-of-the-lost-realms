using System;
using System.Collections;
using UnityEngine;
namespace LostRealms {
 public static class ChapterCleanupChecks {
  public static IEnumerator Run(Action<bool,string> check){
   var g=RealmGame.I;g.LoadLevel(1);yield return new WaitForSeconds(.3f);
   var clips=new System.Collections.Generic.List<AudioClip>();
   foreach(int kind in new[]{8,9,10,21}){
    var clip=g.Audio.GetClip("sfx_boss_roar_"+kind);
    check(clip&&clip.length>1.5f&&!clips.Contains(clip),"Guardian has an independently generated voice: "+kind);
    clips.Add(clip);
   }
   check(g.Audio.GetClip("sfx_boss")!=g.Audio.GetClip("sfx_boss_roar_8"),"Boss impacts do not reuse the roar");
   var island=new GameObject("Island");island.transform.SetParent(g.World.transform);island.transform.position=new Vector3(1000,30,0);
   Art.Shape("Test visible deck",PrimitiveType.Cube,Vector3.down*.25f,new Vector3(4,.5f,4),Color.gray,island.transform,true);
   var proxy=Art.Shape("Test invisible support",PrimitiveType.Cube,Vector3.down*.25f,new Vector3(12,.5f,12),Color.gray,island.transform,true);proxy.GetComponent<Renderer>().enabled=false;
   PushableBlock.Place(island.transform,new Vector3(0,.02f,0),Color.gray);
   var block=island.GetComponentInChildren<PushableBlock>();var body=block.GetComponent<Rigidbody>();
   yield return new WaitForSeconds(.5f);
   check(Mathf.Abs(body.position.y-30)<.1f,"Pushable box rests on visible deck");
   body.position=new Vector3(1001.9f,30.02f,0);body.linearVelocity=Vector3.right*12;body.WakeUp();
   yield return new WaitForSeconds(.8f);
   check(body.position.x>1002.5f&&body.position.y<28.5f,"Pushed box falls past invisible edge support with undamped gravity");
   g.Pause();yield return new WaitForFixedUpdate();var paused=body.position;
   yield return new WaitForSeconds(.25f);
   check(Vector3.Distance(paused,body.position)<.03f,"Falling box freezes while paused");
   g.Resume();yield return new WaitForSeconds(.25f);
   check(body.position.y<paused.y-.2f,"Falling box resumes its vertical velocity");
   UnityEngine.Object.Destroy(block.gameObject);UnityEngine.Object.Destroy(island);
  }
 }
}
