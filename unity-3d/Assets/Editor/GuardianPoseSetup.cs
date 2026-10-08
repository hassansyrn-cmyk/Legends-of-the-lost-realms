using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 public static class GuardianPoseSetup {
  // Author upper-body muscle curves on the standing humanoid pose. All
  // locomotion/root curves remain constant; no Aster asset is written.
  public static void Bake(){
   var source=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Resources/Animations/Aster/idle.anim");
   foreach(string state in new[]{"attack","attack_2","slam","sweep","stomp","eruption","roar","charge_ready","cast","victory"}){
    var clip=UnityEngine.Object.Instantiate(source);clip.name="StoneBrute_"+state;float length=source.length;
    var rest=new Dictionary<string,float>();
    foreach(var b in AnimationUtility.GetCurveBindings(clip)){
     var curve=AnimationUtility.GetEditorCurve(clip,b);float value=curve.Evaluate(0);
     if(b.type==typeof(Animator)){rest[b.propertyName]=value;if(b.propertyName.StartsWith("RootT."))value=b.propertyName=="RootT.y"?.84f:0;}
     AnimationUtility.SetEditorCurve(clip,b,AnimationCurve.Constant(0,length,value));
    }
    void Pose(string muscle,float windup,float strike){
     if(!rest.ContainsKey(muscle)&&Array.IndexOf(HumanTrait.MuscleName,muscle)<0)throw new Exception("Unknown guardian muscle "+muscle);
     float start=rest.TryGetValue(muscle,out var v)?v:0;
     var curve=new AnimationCurve(new Keyframe(0,start),new Keyframe(length*.25f,windup),new Keyframe(length*.48f,windup),new Keyframe(length*.67f,strike),new Keyframe(length*.82f,strike),new Keyframe(length,start));
     AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),muscle),curve);
    }
    string action=state=="cast"?"eruption":state=="victory"?"roar":state;
    foreach(var side in new[]{"Left","Right"})foreach(var finger in new[]{"Thumb","Index","Middle","Ring","Little"})for(int segment=1;segment<=3;segment++){string muscle=side+"Hand."+finger+"."+segment+" Stretched";if(rest.ContainsKey(muscle)&&!(action=="eruption"&&side=="Left"))Pose(muscle,-.85f,-.85f);}
    if(action=="attack"||action=="attack_2"){
     string side=action=="attack"?"Right":"Left";float sign=action=="attack"?1:-1;
     Pose(side+" Arm Down-Up",-.25f,-.1f);Pose(side+" Arm Front-Back",.6f,-.7f);Pose(side+" Forearm Stretch",-.6f,.75f);Pose("Chest Twist Left-Right",sign*-.25f,sign*.3f);
    }else if(action=="slam"){
     foreach(var side in new[]{"Left","Right"}){Pose(side+" Arm Down-Up",.9f,-.8f);Pose(side+" Arm Front-Back",-.25f,0);Pose(side+" Forearm Stretch",.6f,.8f);}
     Pose("Spine Front-Back",-.12f,.22f);
    }else if(action=="sweep"){
     Pose("Right Arm Down-Up",.1f,.1f);Pose("Right Arm Front-Back",.8f,-.85f);Pose("Right Forearm Stretch",.85f,.85f);Pose("Chest Twist Left-Right",-.45f,.45f);Pose("Left Arm Down-Up",-.45f,-.25f);
    }else if(action=="stomp"){
     Pose("Right Upper Leg Front-Back",.32f,0);Pose("Right Lower Leg Stretch",-.3f,.5f);Pose("Right Arm Down-Up",-.35f,-.55f);Pose("Spine Front-Back",-.08f,.12f);
    }else if(action=="eruption"){
     Pose("Left Arm Down-Up",.4f,-.2f);Pose("Left Arm Front-Back",-.6f,-.75f);Pose("Left Forearm Stretch",-.4f,.75f);Pose("Right Arm Down-Up",-.45f,-.25f);Pose("Chest Twist Left-Right",.16f,-.12f);
    }else if(action=="roar"){
     foreach(var side in new[]{"Left","Right"}){Pose(side+" Arm Down-Up",.3f,.45f);Pose(side+" Forearm Stretch",-.7f,-.65f);}
     Pose("Spine Front-Back",-.12f,-.12f);Pose("Head Nod Down-Up",.2f,.3f);
    }else if(action=="charge_ready"){
     Pose("Spine Front-Back",.15f,.18f);foreach(var side in new[]{"Left","Right"}){Pose(side+" Arm Down-Up",-.55f,-.5f);Pose(side+" Forearm Stretch",-.65f,-.65f);}
    }
    var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;settings.mirror=false;AnimationUtility.SetAnimationClipSettings(clip,settings);
    string path="Assets/Resources/Animations/"+clip.name+".anim";var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
    if(old){EditorUtility.CopySerialized(clip,old);UnityEngine.Object.DestroyImmediate(clip);}else AssetDatabase.CreateAsset(clip,path);
    Debug.Log("GUARDIAN_POSE_AUTHORED "+state);
   }
   AssetDatabase.SaveAssets();
  }
 }
}
