using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static Playground.UIFactory;
namespace Playground {
public enum MatchMode { HumanVsLaya, HumanVsTypeSafe, LayaVsTypeSafe }
// Credentials live only in this session. This component never serializes or logs them.
public class MatchSetup:MonoBehaviour {
 public MatchMode Mode{get;private set;}public bool Open{get;private set;}public bool Started{get;private set;}
 public string TypeSafeModel{get;private set;}="jev-latest";
 readonly List<Button> modeButtons=new(),arenaButtons=new(),lengthButtons=new();ArenaPreview preview;Label arenaCaption;
 VisualElement root,cloud;DropdownField matchup,models,layout,matchLength;Toggle timed;TextField key;Label message,versus,explanation;Button start,check,back;bool checking;int validationGeneration;string verifiedKey,sessionKey;string[] verifiedModels;
 static readonly List<string> Modes=new(){"Human vs Laya","Human vs TypeSafe","Laya vs TypeSafe"};
 public void Build(VisualElement parent){
 root=parent;root.AddToClassList("scrim");root.AddToClassList("setup-scrim");
 var shell=Box(root,"setup-shell");
 var hero=Box(shell,"setup-hero");Text(hero,"PHYSICS PLAYGROUND     /     ROOFTOP DUELS","eyebrow hero-kicker");
 Text(hero,"IMPULSE","hero-wordmark");Text(hero,"OWN THE EDGE.","hero-tagline");
 Text(hero,"One rooftop. Two rivals.\nEverything is a weapon.","hero-description");
 preview=new ArenaPreview();hero.Add(preview);var caption=Box(hero,"row preview-caption");arenaCaption=Text(caption,"THE ROOFTOP / ROTATION","eyebrow");var legend=Box(caption,"row");Text(legend,"● YOU  ","eyebrow cyan");Text(legend,"● RIVAL","eyebrow orange");
 var training=Button(hero,"",StartTutorial,"training-card");Text(training,"FIRST TIME ON THE ROOF?","eyebrow cyan");Text(training,"Learn the moves    ↗","training-title");Text(training,"Grab. Throw. Dodge. Send them flying.","muted");
 var heroFooter=Box(hero,"row hero-footer");Text(heroFooter,"01 / STAY UP. SEND THEM DOWN.","eyebrow");Button(heroFooter,"Quit",()=>Application.Quit(),"quiet");
 var card=Box(shell,"setup-card");var scroll=new ScrollView(ScrollViewMode.Vertical);card.Add(scroll);
 Text(scroll,"MAKE IT YOUR MATCH","eyebrow cyan");Text(scroll,"Enter the arena","setup-title");
 Text(scroll,"01  /  CHOOSE YOUR OPPONENT","setup-section");
 matchup=new DropdownField("Matchup",Modes,0){name="matchup"};scroll.Add(matchup);Visible(matchup,false);matchup.RegisterValueChangedCallback(_=>Refresh());
 var modesRow=Box(scroll,"row selection-row");string[] modeTitles={"Play Laya","Play TypeSafe","Watch AI"};string[] modeNotes={"ON THIS MAC","CLOUD OPPONENT","LAYA × TYPESAFE"};
 for(int i=0;i<3;i++){int index=i;var choice=Button(modesRow,"",()=>matchup.index=index,"mode-choice");choice.name="mode-"+i;Text(choice,modeTitles[i],"choice-title");Text(choice,modeNotes[i],"choice-note");modeButtons.Add(choice);}
 var pairing=Box(scroll,"setup-pairing");versus=Text(pairing,"","setup-versus");explanation=Text(pairing,"","description");
 cloud=Box(scroll,"setup-cloud");Text(cloud,"CONNECT TYPESAFE","eyebrow orange");key=new TextField("API key"){isPasswordField=true,name="typesafe-key"};cloud.Add(key);key.RegisterValueChangedCallback(_=>{validationGeneration++;verifiedKey=null;verifiedModels=null;Refresh();});Text(cloud,"Your key stays in this session.","muted");
 models=new DropdownField("Model",new List<string>{"jev-latest"},0){name="typesafe-model"};cloud.Add(models);models.RegisterValueChangedCallback(_=>Refresh());check=Button(cloud,"Connect & load models",()=>StartCoroutine(CheckKey()),"large");check.name="check-typesafe";Button(cloud,"Get an API key ↗",()=>Application.OpenURL("https://console.typesafe.ai"),"quiet");Text(cloud,"TypeSafe receives arena observations and action instructions over HTTPS. Requests use your API account and begin when you enter the arena.","muted");
 Text(scroll,"02  /  PICK YOUR ROOFTOP","setup-section");
 layout=new DropdownField("Arena",new List<string>{"Foundry","Crossfire","Flank","Rotation"},3);scroll.Add(layout);Visible(layout,false);layout.RegisterValueChangedCallback(_=>RefreshSelections());
 var arenasRow=Box(scroll,"row selection-row");string[] arenaNotes={"CLASSIC","CENTRAL PROPS","SIDE ROUTES","ALL THREE"};
 for(int i=0;i<4;i++){int index=i;var choice=Button(arenasRow,"",()=>layout.index=index,"arena-choice");choice.name="arena-"+i;Text(choice,"0"+(i+1),"choice-index");Text(choice,layout.choices[i],"choice-title");Text(choice,arenaNotes[i],"choice-note");arenaButtons.Add(choice);}
 Text(scroll,"03  /  SET THE STAKES","setup-section");
 matchLength=new DropdownField("Match length",new List<string>{"First to 3","First to 5","First to 7"},1);scroll.Add(matchLength);Visible(matchLength,false);matchLength.RegisterValueChangedCallback(_=>RefreshSelections());
 var lengthRow=Box(scroll,"row length-row");Text(lengthRow,"ROUND WINS","eyebrow");for(int i=0;i<3;i++){int index=i;var choice=Button(lengthRow,new[]{"3","5","7"}[i],()=>matchLength.index=index,"length-choice");choice.name="length-"+i;lengthButtons.Add(choice);}
 var ruleRow=Box(scroll,"row rule-row");timed=new Toggle(){value=true,tooltip="Sudden death after 60 seconds"};timed.AddToClassList("rule-toggle");ruleRow.Add(timed);var ruleLabel=Text(ruleRow,"Sudden death after 60 seconds","muted");ruleLabel.pickingMode=PickingMode.Position;ruleLabel.RegisterCallback<ClickEvent>(_=>timed.value=!timed.value);
 var footer=Box(card,"setup-actions");message=Text(footer,"","setup-message");start=Button(footer,"LET’S PLAY    →",StartSelected,"primary launch-button");start.name="start-match";back=Button(footer,"Back to current match",Close,"quiet");
 RefreshSelections();Visible(root,false);
 }
 void RefreshSelections(){for(int i=0;i<modeButtons.Count;i++)modeButtons[i].EnableInClassList("selected",matchup.index==i);for(int i=0;i<arenaButtons.Count;i++)arenaButtons[i].EnableInClassList("selected",layout.index==i);for(int i=0;i<lengthButtons.Count;i++)lengthButtons[i].EnableInClassList("selected",matchLength.index==i);if(preview!=null){preview.Layout=layout.index;Set(arenaCaption,"THE ROOFTOP / "+layout.value.ToUpperInvariant());}}
 public void Show(){if(root==null)return;var a=Arena.Instance;if(a.Tutorial.Running)a.Tutorial.Stop();a.Research.Close(false);a.GetComponent<ExecutorDebugPanel>().Open=false;a.Match.Practice=false;Open=true;a.HUD.Pause(true);a.Match.Generation++;a.Client.CancelPending();a.LeftClient.CancelPending();a.StopExecutors("match_setup");matchup.SetValueWithoutNotify(Modes[(int)Mode]);key.SetValueWithoutNotify(sessionKey??Environment.GetEnvironmentVariable("TYPESAFE_API_KEY")??"");Visible(root,true);Refresh();}
 void Refresh(){if(start==null)return;RefreshSelections();bool remote=matchup.index!=0;Visible(cloud,remote);Visible(back,Started);Set(versus,matchup.index==2?"LAYA    ×    TYPESAFE":matchup.index==1?"YOU    ×    TYPESAFE":"YOU    ×    LAYA");Set(explanation,matchup.index==2?"Watch two independent AI players compete. Cyan is local Laya; orange is TypeSafe. The fixed view keeps the whole arena visible.":matchup.index==1?"Take the cyan corner against TypeSafe's cloud model.":"Take the cyan corner against Laya, running locally on this Mac.");bool verified=verifiedKey==key.value&&verifiedModels!=null&&Array.IndexOf(verifiedModels,models.value)>=0;start.SetEnabled(!checking&&(!remote||verified));check.SetEnabled(!checking&&!string.IsNullOrWhiteSpace(key.value));Set(message,!remote?"ENTER / Start    ·    WASD / Move    ·    F / Push or throw":checking?"Checking TypeSafe access…":verified?"Connected · "+models.value+" · ready to play":"Enter a key and check access before starting.");}
 IEnumerator CheckKey(){checking=true;verifiedKey=null;verifiedModels=null;int version=validationGeneration;string submitted=key.value.Trim();Refresh();yield return LayaClient.DiscoverModels(submitted,(names,error)=>{if(version!=validationGeneration)return;if(error!=null){Set(message,error);return;}verifiedKey=submitted;key.SetValueWithoutNotify(submitted);verifiedModels=names;models.choices=new List<string>(names);models.SetValueWithoutNotify(Array.IndexOf(names,TypeSafeModel)>=0?TypeSafeModel:names[0]);});checking=false;if(version==validationGeneration&&verifiedModels!=null)Refresh();else{start.SetEnabled(matchup.index==0);check.SetEnabled(!string.IsNullOrWhiteSpace(key.value));}}
 public void StartFromKeyboard(){StartSelected();}
 void StartSelected(){if(!Open)return;var mode=(MatchMode)matchup.index;if(mode!=MatchMode.HumanVsLaya&&(verifiedKey!=key.value||verifiedModels==null||Array.IndexOf(verifiedModels,models.value)<0))return;if(!Configure(mode,models.value,key.value))return;Started=true;Open=false;Visible(root,false);Arena.Instance.Match.Layout=layout.index;Arena.Instance.Match.TimedRounds=timed.value;Arena.Instance.Match.WinsRequired=new[]{3,5,7}[matchLength.index];Arena.Instance.Match.NewMatch();Arena.Instance.HUD.Pause(false);key.SetValueWithoutNotify("");}
 public void StartTutorial(){Configure(MatchMode.HumanVsLaya,TypeSafeModel,sessionKey);Started=true;Open=false;Visible(root,false);Arena.Instance.Tutorial.Begin();}
 public bool Configure(MatchMode mode,string model="jev-latest",string credential=null){if(!Enum.IsDefined(typeof(MatchMode),mode)||mode!=MatchMode.HumanVsLaya&&(string.IsNullOrWhiteSpace(credential)||string.IsNullOrWhiteSpace(model)))return false;var a=Arena.Instance;a.Match.Generation++;a.StopExecutors("matchup_changed");a.Client.CancelPending();a.LeftClient.CancelPending();Mode=mode;TypeSafeModel=model;sessionKey=credential;a.Human.Actor=a.LeftName;a.AI.Actor=a.RightName;a.Human.name=a.LeftName;a.AI.name=a.RightName;a.LeftClient.Configure(AIProvider.Laya);a.Client.Configure(mode==MatchMode.HumanVsLaya?AIProvider.Laya:AIProvider.TypeSafe,model,credential);a.LeftBrain.Enabled=mode==MatchMode.LayaVsTypeSafe;a.Brain.Enabled=true;return true;}
 public void Close(){if(!Started)return;validationGeneration++;Open=false;Visible(root,false);key.SetValueWithoutNotify("");Arena.Instance.HUD.Pause(false);}
}
}
