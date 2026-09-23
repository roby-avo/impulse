using System;
using UnityEngine;
namespace Playground {
[Serializable] public class SelfState {public float distance_to_edge_m,speed,balance;public bool near_edge,holding_object,grounded;public string held_object_type;}
[Serializable] public class OpponentState {public float distance_m,distance_to_edge_m,speed;public bool near_edge,holding_object,facing_self;public string held_object_type;}
[Serializable] public class ObjectState {public string type,id,mass_class;public float distance_m;}
[Serializable] public class StateSnapshot {public SelfState self;public OpponentState opponent;public ObjectState nearest_object;public bool incoming_projectile,cover_available;public string previous_action,previous_outcome;}
public static class AIPerception {
 public static StateSnapshot Capture(AIActionExecutor e,ActionContext context){var s=e.Self;var o=e.Opponent;var p=context.Object;
  return new StateSnapshot{self=new SelfState{distance_to_edge_m=Round(s.Edge),near_edge=s.Edge<2,speed=Round(s.Body.linearVelocity.magnitude),holding_object=s.Held,held_object_type=s.Held?s.Held.Kind:"none",grounded=s.Grounded,balance=Time.time<s.StunnedUntil?0:1},opponent=new OpponentState{distance_m=Round(Vector3.Distance(s.transform.position,o.transform.position)),distance_to_edge_m=Round(o.Edge),near_edge=o.Edge<2,holding_object=o.Held,held_object_type=o.Held?o.Held.Kind:"none",speed=Round(o.Body.linearVelocity.magnitude),facing_self=Vector3.Dot(o.transform.forward,(s.transform.position-o.transform.position).normalized)>.5f},nearest_object=new ObjectState{type=p?p.Kind:"none",id=p?p.name:"none",mass_class=p?(p.Body.mass>10?"heavy":"light"):"none",distance_m=p?Round(Vector3.Distance(s.transform.position,p.transform.position)):-1},incoming_projectile=Danger(s),cover_available=context.HasCover,previous_action=e.Outcome=="idle"?"none":e.Selected.ToString(),previous_outcome=e.Outcome};
 }
 static float Round(float v)=>Mathf.Round(v*10)/10;
 public static bool Danger(Fighter self){foreach(var p in Arena.Props){if(!p||p.Holder||p.Owner==self)continue;var d=self.transform.position+Vector3.up-p.transform.position;var v=p.Body.linearVelocity;if(d.magnitude<6&&v.magnitude>4&&Vector3.Dot(d.normalized,v.normalized)>.85f)return true;}return false;}
}
}
