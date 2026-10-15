using System;
using System.IO;
using UnityEngine;
namespace LostRealms {
 public static class RuntimeEvidence {
  static readonly string Run=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,6);
  public static string Folder(int stage)=>Path.Combine(Application.dataPath,"../Validation/ExpansionStage"+stage,"Runs",Run);
 }
}
