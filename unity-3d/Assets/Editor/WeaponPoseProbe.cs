using System.IO;
using UnityEditor;
using UnityEngine;

namespace LostRealms {
 public static class WeaponPoseProbe {
  public static void Validate(){WeaponIconProbe.RunNewWeapons();QualityValidation.Run();}
 }
}
