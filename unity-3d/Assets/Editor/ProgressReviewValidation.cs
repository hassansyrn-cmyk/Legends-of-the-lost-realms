using UnityEditor;
using UnityEngine;
namespace LostRealms {
 public static class ProgressReviewValidation {
  // Run with -realmTest, without -quit. The runtime suite exits when finished.
  public static void Run(){
   if(!RealmGame.Testing)throw new System.InvalidOperationException("Progress review requires -realmTest to protect player saves.");
   for(int id=0;id<=WeaponCatalog.MaxId;id++){
    var definition=WeaponCatalog.Get(id);
    if(!Resources.Load<GameObject>(definition.Resource))throw new System.InvalidOperationException("Missing weapon model: "+definition.Resource);
   }
   Debug.Log("PROGRESS_REVIEW_WEAPON_MODELS_PASSED "+(WeaponCatalog.MaxId+1));
   OrganizeSweep.Run();
   QualityValidation.Run();
  }
 }
}
