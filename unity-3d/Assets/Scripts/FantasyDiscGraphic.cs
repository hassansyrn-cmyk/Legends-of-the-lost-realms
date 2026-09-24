using UnityEngine;
using UnityEngine.UI;
namespace LostRealms {
 // The minimap's circular stencil, not a rectangular clipping approximation.
 public sealed class FantasyDiscGraphic : MaskableGraphic {
  protected override void OnPopulateMesh(VertexHelper mesh){
   mesh.Clear();var rect=rectTransform.rect;Vector2 center=rect.center;float radius=Mathf.Min(rect.width,rect.height)*.5f;
   mesh.AddVert(center,color,Vector2.zero);
   for(int i=0;i<=64;i++){
    float angle=i*Mathf.PI*2/64;mesh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius,color,Vector2.zero);
    if(i>0)mesh.AddTriangle(0,i,i+1);
   }
  }
 }
}