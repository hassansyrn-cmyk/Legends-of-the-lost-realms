using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace LostRealms {
 // Runtime-created MeshColliders must retain their CPU mesh data in a player.
 // Editor Play mode can access imported meshes even when Read/Write is off.
 public sealed class RuntimeCollisionImports:AssetPostprocessor,IPreprocessBuildWithReport {
  public int callbackOrder=>-100;
  static bool RequiresReadWrite(string path){
   if(path.StartsWith("Assets/Resources/Islands/",StringComparison.OrdinalIgnoreCase)||
      path.StartsWith("Assets/Resources/Stormveil/",StringComparison.OrdinalIgnoreCase)||
      path.StartsWith("Assets/Resources/Astral/",StringComparison.OrdinalIgnoreCase)||
      path.StartsWith("Assets/Resources/Props/Crypt/",StringComparison.OrdinalIgnoreCase))return true;
   if(path=="Assets/Resources/Gates/Gate_Storm.fbx"||path=="Assets/Resources/Gates/Gate_Astral.fbx")return true;
   if(!path.StartsWith("Assets/Resources/Props/",StringComparison.OrdinalIgnoreCase))return false;
   string name=Path.GetFileNameWithoutExtension(path);
   // Keep aligned with RealmProps' hollow-building colliders and ExplorationArt's solid rocks.
   return Array.IndexOf(new[]{"Gate_01","House_01","House_02","Ruin_01","Tent_01","Tower_01","Wall_01","Cottage_01","Cottage_02","Bridge_01","rpgpp_lt_shed_wood_01","rpgpp_lt_shed_wood_02","rpgpp_lt_wagon_01","Rock_02"},name)>=0;
  }
  void OnPreprocessModel(){if(RequiresReadWrite(assetPath))((ModelImporter)assetImporter).isReadable=true;}
  [MenuItem("Lost Realms/Quality/Prepare Runtime Collision Meshes")]
  public static void Repair(){
   int changed=0;
   foreach(string guid in AssetDatabase.FindAssets("t:Model",new[]{"Assets/Resources"})){
    string path=AssetDatabase.GUIDToAssetPath(guid);if(!RequiresReadWrite(path))continue;
    var importer=AssetImporter.GetAtPath(path) as ModelImporter;
    if(importer&&!importer.isReadable){importer.isReadable=true;importer.SaveAndReimport();changed++;}
   }
   AssetDatabase.SaveAssets();Debug.Log("RUNTIME_COLLISION_IMPORTS_PREPARED "+changed);
  }
  public void OnPreprocessBuild(BuildReport report){
   foreach(string guid in AssetDatabase.FindAssets("t:Model",new[]{"Assets/Resources"})){
    string path=AssetDatabase.GUIDToAssetPath(guid);if(!RequiresReadWrite(path))continue;
    var importer=AssetImporter.GetAtPath(path) as ModelImporter;
    if(importer&&!importer.isReadable)throw new BuildFailedException("Runtime collision mesh is not readable: "+path+". Run Lost Realms > Quality > Prepare Runtime Collision Meshes before building.");
   }
  }
 }
}
