using System.Collections.Generic;
using UnityEngine;
namespace Playground {
/// <summary>Visual-only Blender models. Never creates or changes physical colliders.</summary>
public static class VisualAssets {
 static readonly Dictionary<string,Material> SharedPalette=new();
 public static int SpawnedModels {get;private set;}
 public static GameObject Spawn(string asset,Transform parent,Vector3 localPosition,Color team){
  var prefab=Resources.Load<GameObject>("Visuals/"+asset);
  if(!prefab)throw new System.InvalidOperationException("Missing authored visual asset: "+asset);
  var model=Object.Instantiate(prefab,parent,false);model.name="Art / "+asset;model.transform.localPosition=localPosition;
  var palette=SharedPalette;
  foreach(var renderer in model.GetComponentsInChildren<Renderer>()){
   var original=renderer.sharedMaterials;var assigned=new Material[original.Length];
   for(int i=0;i<assigned.Length;i++){string key=original[i]?original[i].name.Split('.')[0]:"Metal";
    string cacheKey=key+((key=="Team"||key=="Glow")?team.ToString():"");
    if(!palette.TryGetValue(cacheKey,out var material)){Color color=key switch{
     "Team"=>team,"Shell"=>new Color(.72f,.79f,.79f),"Metal"=>new Color(.16f,.23f,.28f),
     "Dark"=>new Color(.025f,.055f,.075f),"Glow"=>Color.Lerp(team,Color.white,.7f),
     "Amber"=>new Color(1,.56f,.12f),"Wood"=>new Color(.42f,.22f,.09f),"Rubber"=>new Color(.035f,.04f,.05f),
     "Deck"=>new Color(.12f,.19f,.23f),"Paint"=>new Color(.32f,.42f,.46f),"Window"=>new Color(.35f,.61f,.70f),_=>new Color(.35f,.43f,.48f)};
     material=Arena.Material(color,key=="Glow"||key=="Window");material.name="Authored / "+key;material.SetFloat("_Smoothness",key=="Metal"?.55f:.3f);palette[cacheKey]=material;
    }assigned[i]=material;
   }renderer.sharedMaterials=assigned;
  }
  Combine(model);SpawnedModels++;return model;
 }
 static void Combine(GameObject model){
  var groups=new Dictionary<Material,List<CombineInstance>>();
  foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>()){
   var filter=renderer.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh)continue;
   for(int i=0;i<renderer.sharedMaterials.Length;i++){
    var mat=renderer.sharedMaterials[i];if(!groups.TryGetValue(mat,out var group)){group=new List<CombineInstance>();groups[mat]=group;}
    group.Add(new CombineInstance{mesh=filter.sharedMesh,subMeshIndex=i,transform=model.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix});
   }renderer.enabled=false;
  }
  foreach(var group in groups){var child=new GameObject("Batched / "+group.Key.name);child.transform.SetParent(model.transform,false);var mesh=new Mesh{name=model.name+" / merged"};mesh.CombineMeshes(group.Value.ToArray(),true,true);mesh.RecalculateBounds();child.AddComponent<MeshFilter>().sharedMesh=mesh;child.AddComponent<MeshRenderer>().sharedMaterial=group.Key;}
 }
 public static void SkinProxy(GameObject proxy,string asset,Color team){
  proxy.GetComponent<Renderer>().enabled=false;
  var model=Spawn(asset,proxy.transform,Vector3.zero,team);
  var s=proxy.transform.localScale;model.transform.localScale=new Vector3(1/s.x,1/s.y,1/s.z);
 }
}
}
