using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static Playground.UIFactory;
namespace Playground {
public enum MatchMode { HumanVsLaya, HumanVsTypeSafe, LayaVsTypeSafe }
// Credentials stay in memory unless the player opts in to the macOS Keychain (CredentialStore).
// This component never writes them to PlayerPrefs, logs, telemetry or exports.
public class MatchSetup:MonoBehaviour {
 public MatchMode Mode{get;private set;}public bool Open{get;private set;}public bool Started{get;private set;}
 public string TypeSafeModel{get;private set;}="jev-latest";
 readonly List<Button> modeButtons=new(),arenaButtons=new(),lengthButtons=new();ArenaPreview preview;Label arenaCaption;
 VisualElement root,cloud,keyRow;DropdownField matchup,models,layout,matchLength;Toggle timed,remember;TextField key;Label message,versus,explanation,keyNote;Button start,check,back,forget;bool checking,savedLoaded;int validationGeneration;string verifiedKey,sessionKey,savedKey;string[] verifiedModels;
 public string DiscoveryEndpoint=LayaClient.TypeSafeEndpoint;
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
 matchup=new DropdownField("Matchup",Modes,0){name="matchup"};scroll.Add(matchup);Visible(matchup,false);matchup.RegisterValueChangedCallback(_=>{FillKey();Refresh();AutoConnect();});
 var modesRow=Box(scroll,"row selection-row");string[] modeTitles={"Play Laya","Play TypeSafe","Watch AI"};string[] modeNotes={"ON THIS MAC","CLOUD OPPONENT","LAYA × TYPESAFE"};
 for(int i=0;i<3;i++){int index=i;var choice=Button(modesRow,"",()=>matchup.index=index,"mode-choice");choice.name="mode-"+i;Text(choice,modeTitles[i],"choice-title");Text(choice,modeNotes[i],"choice-note");modeButtons.Add(choice);}
 var pairing=Box(scroll,"setup-pairing");versus=Text(pairing,"","setup-versus");explanation=Text(pairing,"","description");
 cloud=Box(scroll,"setup-cloud");Text(cloud,"CONNECT TYPESAFE","eyebrow orange");key=new TextField("API key"){isPasswordField=true,name="typesafe-key"};cloud.Add(key);key.RegisterValueChangedCallback(_=>{validationGeneration++;verifiedKey=null;verifiedModels=null;Refresh();});
 keyRow=Box(cloud,"row rule-row");remember=new Toggle(){value=PlayerPrefs.GetInt("remember_key",1)==1,name="remember-key"};remember.AddToClassList("rule-toggle");keyRow.Add(remember);var rememberLabel=Text(keyRow,"Remember key on this Mac","muted");rememberLabel.pickingMode=PickingMode.Position;rememberLabel.RegisterCallback<ClickEvent>(_=>remember.value=!remember.value);remember.RegisterValueChangedCallback(e=>SetRemember(e.newValue));forget=Button(keyRow,"Forget saved key",Forget,"quiet");forget.name="forget-key";keyNote=Text(cloud,"","muted");
 models=new DropdownField("Model",new List<string>{"jev-latest"},0){name="typesafe-model"};cloud.Add(models);models.RegisterValueChangedCallback(_=>Refresh());check=Button(cloud,"Connect & load models",()=>StartCoroutine(CheckKey()),"large");check.name="check-typesafe";Button(cloud,"Get an API key ↗",()=>Application.OpenURL("https://console.typesafe.ai"),"quiet");Text(cloud,"TypeSafe receives arena observations and action instructions over HTTPS. Requests use your API account and begin when you enter the arena.","muted");
 Text(scroll,"02  /  PICK YOUR ROOFTOP","setup-section");
 layout=new DropdownField("Arena",new List<string>{"Foundry","Crossfire","Flank","Rotation"},3){name="arena"};scroll.Add(layout);Visible(layout,false);layout.RegisterValueChangedCallback(_=>RefreshSelections());
 var arenasRow=Box(scroll,"row selection-row");string[] arenaNotes={"CLASSIC","CENTRAL PROPS","SIDE ROUTES","ALL THREE"};
 for(int i=0;i<4;i++){int index=i;var choice=Button(arenasRow,"",()=>layout.index=index,"arena-choice");choice.name="arena-"+i;Text(choice,"0"+(i+1),"choice-index");Text(choice,layout.choices[i],"choice-title");Text(choice,arenaNotes[i],"choice-note");arenaButtons.Add(choice);}
 Text(scroll,"03  /  SET THE STAKES","setup-section");
 matchLength=new DropdownField("Match length",new List<string>{"First to 3","First to 5","First to 7"},1){name="match-length"};scroll.Add(matchLength);Visible(matchLength,false);matchLength.RegisterValueChangedCallback(_=>RefreshSelections());
 var lengthRow=Box(scroll,"row length-row");Text(lengthRow,"ROUND WINS","eyebrow");for(int i=0;i<3;i++){int index=i;var choice=Button(lengthRow,new[]{"3","5","7"}[i],()=>matchLength.index=index,"length-choice");choice.name="length-"+i;lengthButtons.Add(choice);}
 var ruleRow=Box(scroll,"row rule-row");timed=new Toggle(){value=true,tooltip="Sudden death after 60 seconds"};timed.AddToClassList("rule-toggle");ruleRow.Add(timed);var ruleLabel=Text(ruleRow,"Sudden death after 60 seconds","muted");ruleLabel.pickingMode=PickingMode.Position;ruleLabel.RegisterCallback<ClickEvent>(_=>timed.value=!timed.value);
 var footer=Box(card,"setup-actions");message=Text(footer,"","setup-message");start=Button(footer,"LET’S PLAY    →",StartSelected,"primary launch-button");start.name="start-match";back=Button(footer,"Back to current match",Close,"quiet");
 Restore();RefreshSelections();Visible(root,false);
 }
 // Non-secret choices persist in PlayerPrefs. The API key never does.
 void Restore(){matchup.SetValueWithoutNotify(Modes[Mathf.Clamp(PlayerPrefs.GetInt("setup_mode",0),0,Modes.Count-1)]);layout.SetValueWithoutNotify(layout.choices[Mathf.Clamp(PlayerPrefs.GetInt("setup_layout",3),0,layout.choices.Count-1)]);matchLength.SetValueWithoutNotify(matchLength.choices[Mathf.Clamp(PlayerPrefs.GetInt("setup_length",1),0,matchLength.choices.Count-1)]);timed.SetValueWithoutNotify(PlayerPrefs.GetInt("setup_timed",1)==1);TypeSafeModel=PlayerPrefs.GetString("typesafe_model",TypeSafeModel);}
 void SaveChoices(){PlayerPrefs.SetInt("setup_mode",matchup.index);PlayerPrefs.SetInt("setup_layout",layout.index);PlayerPrefs.SetInt("setup_length",matchLength.index);PlayerPrefs.SetInt("setup_timed",timed.value?1:0);PlayerPrefs.SetString("typesafe_model",TypeSafeModel);PlayerPrefs.Save();}
 string SavedKey(){if(!savedLoaded){savedLoaded=true;savedKey=CredentialStore.Load();}return savedKey;}
 // Reading the Keychain only when a cloud matchup is selected avoids prompts for local play.
 void FillKey(){if(matchup.index==0||!string.IsNullOrEmpty(key.value))return;var value=sessionKey??Environment.GetEnvironmentVariable("TYPESAFE_API_KEY")??(remember.value?SavedKey():null);if(!string.IsNullOrEmpty(value))key.SetValueWithoutNotify(value);}
 void AutoConnect(){if(Open&&matchup.index!=0&&!checking&&!string.IsNullOrWhiteSpace(key.value)&&verifiedKey!=key.value.Trim())StartCoroutine(CheckKey());}
 void SetRemember(bool value){PlayerPrefs.SetInt("remember_key",value?1:0);if(value){if(verifiedKey!=null&&CredentialStore.Save(verifiedKey))savedKey=verifiedKey;}else{CredentialStore.Delete();savedKey=null;savedLoaded=true;}Refresh();}
 public void Forget(){CredentialStore.Delete();if(key.value.Trim()==savedKey)key.value="";savedKey=null;savedLoaded=true;Refresh();}
 void RefreshSelections(){for(int i=0;i<modeButtons.Count;i++)modeButtons[i].EnableInClassList("selected",matchup.index==i);for(int i=0;i<arenaButtons.Count;i++)arenaButtons[i].EnableInClassList("selected",layout.index==i);for(int i=0;i<lengthButtons.Count;i++)lengthButtons[i].EnableInClassList("selected",matchLength.index==i);if(preview!=null){preview.Layout=layout.index;Set(arenaCaption,"THE ROOFTOP / "+layout.value.ToUpperInvariant());}}
 public void Show(){if(root==null)return;var a=Arena.Instance;if(a.Tutorial.Running)a.Tutorial.Stop();a.Research.Close(false);a.GetComponent<ExecutorDebugPanel>().Open=false;a.Match.Practice=false;Open=true;a.HUD.Pause(true);a.Match.Generation++;a.Client.CancelPending();a.LeftClient.CancelPending();a.StopExecutors("match_setup");if(Started)matchup.SetValueWithoutNotify(Modes[(int)Mode]);key.SetValueWithoutNotify("");FillKey();Visible(root,true);Refresh();AutoConnect();}
 void Refresh(){if(start==null)return;RefreshSelections();bool remote=matchup.index!=0;Visible(cloud,remote);Visible(back,Started);Set(versus,matchup.index==2?"LAYA    ×    TYPESAFE":matchup.index==1?"YOU    ×    TYPESAFE":"YOU    ×    LAYA");Set(explanation,matchup.index==2?"Watch two independent AI players compete. Cyan is local Laya; orange is TypeSafe. The fixed view keeps the whole arena visible.":matchup.index==1?"Take the cyan corner against TypeSafe's cloud model.":"Take the cyan corner against Laya, running locally on this Mac.");bool verified=verifiedKey==key.value&&verifiedModels!=null&&Array.IndexOf(verifiedModels,models.value)>=0;start.SetEnabled(!checking&&(!remote||verified));check.SetEnabled(!checking&&!string.IsNullOrWhiteSpace(key.value));Visible(keyRow,CredentialStore.Persistent);Visible(forget,!string.IsNullOrEmpty(savedKey));Set(keyNote,!CredentialStore.Persistent||!remember.value?"Your key stays in this session only.":savedKey!=null&&savedKey==key.value.Trim()?"Saved in your macOS Keychain. Remove it any time with Forget saved key.":"After it connects, the key is saved in your macOS Keychain.");Set(message,!remote?"ENTER / Start    ·    WASD / Move    ·    F / Push or throw":checking?"Checking TypeSafe access…":verified?"Connected · "+models.value+" · ready to play":"Enter a key and check access before starting.");}
 public IEnumerator CheckKey(){checking=true;verifiedKey=null;verifiedModels=null;int version=validationGeneration;string submitted=key.value.Trim();Refresh();yield return LayaClient.DiscoverModels(submitted,(names,error)=>{if(version!=validationGeneration)return;if(error!=null){Set(message,error);return;}verifiedKey=submitted;if(remember.value&&CredentialStore.Persistent&&savedKey!=submitted&&CredentialStore.Save(submitted))savedKey=submitted;key.SetValueWithoutNotify(submitted);verifiedModels=names;models.choices=new List<string>(names);models.SetValueWithoutNotify(Array.IndexOf(names,TypeSafeModel)>=0?TypeSafeModel:names[0]);},DiscoveryEndpoint);checking=false;if(version==validationGeneration&&verifiedModels!=null)Refresh();else{start.SetEnabled(matchup.index==0);check.SetEnabled(!string.IsNullOrWhiteSpace(key.value));}}
 public void StartFromKeyboard(){StartSelected();}
 void StartSelected(){if(!Open)return;var mode=(MatchMode)matchup.index;if(mode!=MatchMode.HumanVsLaya&&(verifiedKey!=key.value||verifiedModels==null||Array.IndexOf(verifiedModels,models.value)<0))return;if(!Configure(mode,models.value,key.value))return;Started=true;SaveChoices();Open=false;Visible(root,false);Arena.Instance.Match.Layout=layout.index;Arena.Instance.Match.TimedRounds=timed.value;Arena.Instance.Match.WinsRequired=new[]{3,5,7}[matchLength.index];Arena.Instance.Match.NewMatch();Arena.Instance.HUD.Pause(false);key.SetValueWithoutNotify("");}
 public void StartTutorial(){Configure(MatchMode.HumanVsLaya,TypeSafeModel,sessionKey);Started=true;Open=false;Visible(root,false);Arena.Instance.Tutorial.Begin();}
 public bool Configure(MatchMode mode,string model="jev-latest",string credential=null){if(!Enum.IsDefined(typeof(MatchMode),mode)||mode!=MatchMode.HumanVsLaya&&(string.IsNullOrWhiteSpace(credential)||string.IsNullOrWhiteSpace(model)))return false;var a=Arena.Instance;a.Match.Generation++;a.StopExecutors("matchup_changed");a.Client.CancelPending();a.LeftClient.CancelPending();Mode=mode;TypeSafeModel=model;sessionKey=credential;a.Human.Actor=a.LeftName;a.AI.Actor=a.RightName;a.Human.name=a.LeftName;a.AI.name=a.RightName;a.LeftClient.Configure(AIProvider.Laya);a.Client.Configure(mode==MatchMode.HumanVsLaya?AIProvider.Laya:AIProvider.TypeSafe,model,credential);a.LeftBrain.Enabled=mode==MatchMode.LayaVsTypeSafe;a.Brain.Enabled=true;return true;}
 public void Close(){if(!Started)return;validationGeneration++;Open=false;Visible(root,false);key.SetValueWithoutNotify("");Arena.Instance.HUD.Pause(false);}
}
}
