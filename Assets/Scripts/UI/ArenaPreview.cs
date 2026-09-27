using UnityEngine;
using UnityEngine.UIElements;
namespace Playground {
// Resolution-independent menu illustration. Deliberately schematic, not a minimap.
public sealed class ArenaPreview:VisualElement {
 int layout=3;public int Layout{get=>layout;set{if(layout==value)return;layout=value;MarkDirtyRepaint();}}
 static readonly Color Cyan=new Color(.40f,.91f,.91f),Orange=new Color(1,.48f,.34f);
 public ArenaPreview(){AddToClassList("arena-preview");pickingMode=PickingMode.Ignore;generateVisualContent+=Draw;}
 void Draw(MeshGenerationContext context){
  float w=contentRect.width,h=contentRect.height;if(w<1||h<1)return;var p=context.painter2D;
  Vector2 Iso(float x,float y,float z=0)=>new Vector2(w*.5f+(x-y)*w*.022f,h*.48f+(x+y)*h*.024f-z);
  void Line(Vector2 a,Vector2 b,Color color,float width=1){p.strokeColor=color;p.lineWidth=width;p.BeginPath();p.MoveTo(a);p.LineTo(b);p.Stroke();}
  void Poly(Color color,params Vector2[] points){p.fillColor=color;p.BeginPath();p.MoveTo(points[0]);for(int i=1;i<points.Length;i++)p.LineTo(points[i]);p.ClosePath();p.Fill();}
  void Dot(Vector2 at,float r,Color color){p.fillColor=color;p.BeginPath();p.Arc(at,r,0,360);p.Fill();}
  var a=Iso(-10,-10);var b=Iso(10,-10);var c=Iso(10,10);var d=Iso(-10,10);
  Poly(new Color(.06f,.12f,.16f),a,b,c,d);Poly(new Color(.04f,.08f,.11f),d,c,c+Vector2.up*18,d+Vector2.up*18);Poly(new Color(.08f,.16f,.20f),b,c,c+Vector2.up*18,b+Vector2.up*18);
  for(int i=-8;i<=8;i+=2){Line(Iso(i,-10),Iso(i,10),new Color(.14f,.23f,.27f));Line(Iso(-10,i),Iso(10,i),new Color(.14f,.23f,.27f));}
  for(int i=-10;i<10;i+=2){Line(Iso(i,-10),Iso(i+1,-10),new Color(.68f,.75f,.49f),2);Line(Iso(-10,i),Iso(-10,i+1),new Color(.68f,.75f,.49f),2);}
  Line(b,c,Cyan,2);Line(c,d,Cyan,2);
  void Block(float x,float y,float sx,float sy,float tall,Color color){var v=Iso(x-sx,y-sy);var q=Iso(x+sx,y-sy);var r=Iso(x+sx,y+sy);var t=Iso(x-sx,y+sy);var lift=Vector2.up*tall;Poly(color*.65f,t,r,r-lift,t-lift);Poly(color*.8f,q,r,r-lift,q-lift);Poly(color,v-lift,q-lift,r-lift,t-lift);}
  Block(-4,2,1.3f,1,17,new Color(.42f,.54f,.56f));Block(4,-2,1.3f,1,17,new Color(.42f,.54f,.56f));
  float spread=layout==1?2:layout==2?6:4;Block(-spread,-3,.5f,.5f,10,Orange);Block(spread,3,.5f,.5f,10,Orange);Block(2,-5,.5f,.5f,7,new Color(.81f,.78f,.58f));Block(-2,5,.5f,.5f,7,new Color(.81f,.78f,.58f));
  var human=Iso(0,6,8);var rival=Iso(0,-6,8);p.lineWidth=1;p.strokeColor=new Color(.4f,.91f,.91f,.45f);p.BeginPath();p.MoveTo(human);p.BezierCurveTo(human+new Vector2(-45,-65),rival+new Vector2(-35,-60),rival);p.Stroke();
  Dot(human,13,new Color(.1f,.28f,.31f));Dot(human,6,Cyan);Dot(rival,13,new Color(.32f,.17f,.14f));Dot(rival,6,Orange);
 }
}
}
