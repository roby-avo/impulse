using System;
using System.Text;
using System.Globalization;
using UnityEngine;
namespace Playground {
[Serializable] public class SelfState {public float distance_to_edge_m,speed,balance,push_ready_in,dodge_ready_in,knockback_bonus;public bool near_edge,holding_object,grounded;public string held_object_type;}
[Serializable] public class OpponentState {public float distance_m,distance_to_edge_m,speed;public bool near_edge,holding_object,facing_self;public string held_object_type;public bool winding_up,recovering;public float edge_behind_m,knockback_bonus;}
[Serializable] public class ObjectState {public string type,id,mass_class;public float distance_m;}
[Serializable] public class StateSnapshot {public string situation;public SelfState self;public OpponentState opponent;public ObjectState nearest_object;public bool incoming_projectile,cover_available,sudden_death;public string previous_action,previous_outcome;}
public static class AIPerception {
 public static StateSnapshot Capture(AIActionExecutor e,ActionContext context){var s=e.Self;var o=e.Opponent;var p=context.Object;
  return new StateSnapshot{situation=Describe(e,context),self=new SelfState{distance_to_edge_m=Round(s.Edge),near_edge=s.Edge<2,speed=Round(s.Body.linearVelocity.magnitude),holding_object=s.Held,held_object_type=s.Held?s.Held.Kind:"none",grounded=s.Grounded,balance=Time.time<s.StunnedUntil?0:1,push_ready_in=Round(Mathf.Max(0,s.NextPush-Time.time)),dodge_ready_in=Round(Mathf.Max(0,s.NextDodge-Time.time)),knockback_bonus=Round(s.KnockbackBonus)},opponent=new OpponentState{winding_up=o.WindingUp,recovering=Time.time<o.RecoveryUntil,distance_m=Round(Vector3.Distance(s.transform.position,o.transform.position)),distance_to_edge_m=Round(o.Edge),edge_behind_m=Round(EdgeBehind(s,o)),knockback_bonus=Round(o.KnockbackBonus),near_edge=o.Edge<2,holding_object=o.Held,held_object_type=o.Held?o.Held.Kind:"none",speed=Round(o.Body.linearVelocity.magnitude),facing_self=Vector3.Dot(o.transform.forward,(s.transform.position-o.transform.position).normalized)>.5f},nearest_object=new ObjectState{type=p?p.Kind:"none",id=p?p.name:"none",mass_class=p?(p.Body.mass>10?"heavy":"light"):"none",distance_m=p?Round(Vector3.Distance(s.transform.position,p.transform.position)):-1},incoming_projectile=Danger(s),sudden_death=Arena.Instance.Match.SuddenDeath,cover_available=context.HasCover,previous_action=e.Outcome=="idle"?"none":e.Selected.ToString(),previous_outcome=e.Outcome};
 }
 // Observable facts in plain language help the compact local classifier interpret geometry.
 // This is an independently selectable observation, never a recommended or substituted action.
 public static string Describe(AIActionExecutor e,ActionContext context){
  var s=e.Self;var o=e.Opponent;var p=context.Object;float distance=Vector3.Distance(s.transform.position,o.transform.position);
  var b=new StringBuilder();
  if(s.CanLedgeSave)b.Append("I just slipped off the edge; jumping right now can save me. ");
  else if(s.Edge<0)b.Append("I am outside the safe boundary and about to lose the round. ");
  else if(s.Edge<1.4f)b.Append("I am dangerously close to falling off the roof. ");
  if(Danger(s))b.Append("A fast thrown object is flying toward me and will hit me soon. ");
  if(o.WindingUp&&distance<3&&Vector3.Dot(o.transform.forward,(s.transform.position-o.transform.position).normalized)>.5f)b.Append("The enemy is winding up an attack that will hit me soon. ");
  else if(Time.time<o.RecoveryUntil)b.Append("The enemy is recovering after an attack. ");
  if(s.Held)b.Append("I am holding a ").Append(s.Held.Kind).Append(". ");
  else b.Append("I am empty handed. ");
  if(distance<=Fighter.PushRange)b.Append("The enemy is within pushing range. ");
  else b.Append("The enemy is ").Append(distance.ToString("0.0",CultureInfo.InvariantCulture)).Append(" meters away. ");
  if(o.Edge<2)b.Append("The enemy is near an open edge. ");
  float behind=EdgeBehind(s,o);if(behind<2.5f)b.Append("A shove now would push the enemy toward the edge, only ").Append(Mathf.Max(0,behind).ToString("0.0",CultureInfo.InvariantCulture)).Append(" meters behind them. ");else if(o.Edge<3)b.Append("I am not between the enemy and the roof center. ");
  if(o.Held)b.Append("The enemy is holding a throwable weapon. ");
  if(!s.Held&&p){if(Vector3.Distance(s.transform.position+Vector3.up*.7f,p.transform.position)<=Fighter.GrabRange)b.Append("A throwable ").Append(p.Kind).Append(" is within my reach. ");else b.Append("The nearest throwable weapon is ").Append(Vector3.Distance(s.transform.position,p.transform.position).ToString("0.0",CultureInfo.InvariantCulture)).Append(" meters away. ");}
  if(s.KnockbackBonus>=.24f)b.Append("I have taken several hits, so the next one will send me farther. ");
  if(o.KnockbackBonus>=.24f)b.Append("The enemy has taken several hits, so my next shove will send them farther. ");
  if(s.NextPush>Time.time)b.Append("My shove is cooling down. ");
  if(Physics.Linecast(s.transform.position+Vector3.up,o.transform.position+Vector3.up,1<<8))b.Append("Solid cover blocks the line to the enemy.");
  else if(s.Held)b.Append("There is a clear throwing lane.");
  return b.ToString().TrimEnd();
 }
 static float Round(float v)=>Mathf.Round(v*10)/10;
 // Roof left behind the opponent along the line from me through them: how far a shove from here
 // would have to carry them before they leave the safe area.
 public static float EdgeBehind(Fighter self,Fighter opponent){var o=Vector3.ProjectOnPlane(opponent.transform.position,Vector3.up);var d=Vector3.ProjectOnPlane(opponent.transform.position-self.transform.position,Vector3.up);if(d.sqrMagnitude<.0001f)return opponent.Edge;d.Normalize();float extent=Arena.Instance.Match.SafeHalfExtent,t=float.PositiveInfinity;
  if(Mathf.Abs(d.x)>1e-4f)t=Mathf.Min(t,((d.x>0?extent:-extent)-o.x)/d.x);if(Mathf.Abs(d.z)>1e-4f)t=Mathf.Min(t,((d.z>0?extent:-extent)-o.z)/d.z);return Mathf.Max(-1,t);}
 public static bool Danger(Fighter self){
  var center=self.transform.position+Vector3.up*.9f;
  foreach(var p in Arena.Props){
   if(!p||p.Holder||p.Owner==self||p.Body.isKinematic)continue;
   var delta=center-p.Body.position;var velocity=p.Body.linearVelocity-self.Body.linearVelocity;
   if(delta.sqrMagnitude>144||velocity.sqrMagnitude<16||Vector3.Dot(delta,velocity)<=0)continue;
   // Observe near-future collision geometry, not just whether something moves generally toward us.
   float t=Mathf.Clamp(Vector3.Dot(delta,velocity)/velocity.sqrMagnitude,0,.65f);
   var miss=delta-velocity*t-Physics.gravity*(.5f*t*t);
   if(miss.sqrMagnitude<1.7f)return true;
  }
  return false;
 }
}
}
