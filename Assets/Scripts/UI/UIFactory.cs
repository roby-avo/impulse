using System;
using UnityEngine;
using UnityEngine.UIElements;
namespace Playground {
public static class UIFactory {
 public static VisualElement Box(VisualElement parent,string classes=""){var e=new VisualElement();foreach(var c in classes.Split(' '))if(c.Length>0)e.AddToClassList(c);parent.Add(e);return e;}
 public static Label Text(VisualElement parent,string text,string classes=""){var e=new Label(text){pickingMode=PickingMode.Ignore};foreach(var c in classes.Split(' '))if(c.Length>0)e.AddToClassList(c);parent.Add(e);return e;}
 public static Button Button(VisualElement parent,string text,Action action,string classes=""){var e=new Button(action){text=text};foreach(var c in classes.Split(' '))if(c.Length>0)e.AddToClassList(c);parent.Add(e);return e;}
 public static void Visible(VisualElement e,bool show){if(e!=null){var next=show?DisplayStyle.Flex:DisplayStyle.None;if(e.style.display.value!=next)e.style.display=next;}}
 public static void Set(Label e,string value){if(e.text!=value)e.text=value;}
}
}
