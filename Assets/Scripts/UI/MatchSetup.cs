using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static Playground.UIFactory;
namespace Playground {
public enum MatchMode { HumanVsLaya, HumanVsTypeSafe, AIVsAI }
// One AI player: a provider and the model it runs. Local models are catalog ids served by ai/server.py.
[Serializable] public struct Seat {
 public AIProvider Provider;public string Model;
 public Seat(AIProvider provider,string model){Provider=provider;Model=model;}
 public static Seat Laya(string model)=>new(AIProvider.Laya,model);public static Seat TypeSafe(string model)=>new(AIProvider.TypeSafe,model);
 public string Id=>(Provider==AIProvider.Laya?"laya:":"typesafe:")+Model;
 public string ProviderName=>Provider==AIProvider.Laya?"Laya":"TypeSafe";
 public static Seat Parse(string id,Seat fallback){if(string.IsNullOrEmpty(id))return fallback;int split=id.IndexOf(':');if(split<1||split==id.Length-1)return fallback;var model=id.Substring(split+1);return id.StartsWith("laya:")?Laya(model):id.StartsWith("typesafe:")?TypeSafe(model):fallback;}
}
// Credentials stay in memory unless the player opts in to the macOS Keychain (CredentialStore).
// This component never writes them to PlayerPrefs, logs, telemetry or exports.
public class MatchSetup:MonoBehaviour {
 public MatchMode Mode{get;private set;}public bool Open{get;private set;}public bool Started{get;private set;}
 public string TypeSafeModel{get;private set;}="jev-latest";public string LayaModel{get;private set;}="english";
 public Seat LeftSeat{get;private set;}public Seat RightSeat{get;private set;}=Seat.Laya("english");
 public string LeftName{get;private set;}="Human";public string RightName{get;private set;}="Laya";
 public LocalModelList Local{get;private set;}
 readonly List<Button> modeButtons=new(),arenaButtons=new(),lengthButtons=new();ArenaPreview preview;Label arenaCaption;
 VisualElement root,cloud,keyRow,localBox;DropdownField matchup,models,layout,matchLength,layaModel,watchLeft,watchRight;Toggle timed,remember;TextField key;Label message,versus,explanation,keyNote,localTitle,localInfo;Button start,check,back,forget,download;bool checking,savedLoaded;int validationGeneration;string verifiedKey,sessionKey,savedKey,localError,watchLeftId="laya:english",watchRightId,downloadTarget;string[] verifiedModels;
 readonly List<string> localIds=new(),seatIds=new();
 public string DiscoveryEndpoint=LayaClient.TypeSafeEndpoint;
 static readonly List<string> Modes=new(){"Human vs Laya","Human vs TypeSafe","AI vs AI"};
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
 matchup=new DropdownField("Matchup",Modes,0){name="matchup"};scroll.Add(matchup);Visible(matchup,false);matchup.RegisterValueChangedCallback(_=>{FillKey();Refresh();AutoConnect();Warm();});
 var modesRow=Box(scroll,"row selection-row");string[] modeTitles={"Play Laya","Play TypeSafe","Watch AI"};string[] modeNotes={"ON THIS MAC","CLOUD OPPONENT","ANY TWO MODELS"};
 for(int i=0;i<3;i++){int index=i;var choice=Button(modesRow,"",()=>matchup.index=index,"mode-choice");choice.name="mode-"+i;Text(choice,modeTitles[i],"choice-title");Text(choice,modeNotes[i],"choice-note");modeButtons.Add(choice);}
 var pairing=Box(scroll,"setup-pairing");versus=Text(pairing,"","setup-versus");explanation=Text(pairing,"","description");
 localBox=Box(scroll,"setup-cloud setup-local");localTitle=Text(localBox,"","eyebrow cyan");
 layaModel=new DropdownField("Laya model",new List<string>{"english"},0){name="laya-model"};localBox.Add(layaModel);layaModel.RegisterValueChangedCallback(_=>{if(layaModel.index>=0&&layaModel.index<localIds.Count)LayaModel=localIds[layaModel.index];Warm();Refresh();});
 watchLeft=new DropdownField("Cyan player",new List<string>(),0){name="watch-left"};localBox.Add(watchLeft);watchLeft.RegisterValueChangedCallback(_=>{if(watchLeft.index>=0&&watchLeft.index<seatIds.Count)watchLeftId=seatIds[watchLeft.index];SeatChanged();});
 watchRight=new DropdownField("Orange player",new List<string>(),0){name="watch-right"};localBox.Add(watchRight);watchRight.RegisterValueChangedCallback(_=>{if(watchRight.index>=0&&watchRight.index<seatIds.Count)watchRightId=seatIds[watchRight.index];SeatChanged();});
 localInfo=Text(localBox,"","muted local-info");download=Button(localBox,"",Download,"large");download.name="download-model";Text(localBox,"Add your own checkpoints with ai/models.py (see ai/README.md).","muted");
 cloud=Box(scroll,"setup-cloud");Text(cloud,"CONNECT TYPESAFE","eyebrow orange");key=new TextField("API key"){isPasswordField=true,name="typesafe-key"};cloud.Add(key);key.RegisterValueChangedCallback(_=>{validationGeneration++;verifiedKey=null;verifiedModels=null;RebuildChoices();Refresh();});
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
 Restore();RebuildChoices();RefreshSelections();Visible(root,false);StartCoroutine(PollLocal());
 }
 // Non-secret choices persist in PlayerPrefs. The API key never does.
 void Restore(){matchup.SetValueWithoutNotify(Modes[Mathf.Clamp(PlayerPrefs.GetInt("setup_mode",0),0,Modes.Count-1)]);layout.SetValueWithoutNotify(layout.choices[Mathf.Clamp(PlayerPrefs.GetInt("setup_layout",3),0,layout.choices.Count-1)]);matchLength.SetValueWithoutNotify(matchLength.choices[Mathf.Clamp(PlayerPrefs.GetInt("setup_length",1),0,matchLength.choices.Count-1)]);timed.SetValueWithoutNotify(PlayerPrefs.GetInt("setup_timed",1)==1);TypeSafeModel=PlayerPrefs.GetString("typesafe_model",TypeSafeModel);LayaModel=PlayerPrefs.GetString("laya_model",LayaModel);watchLeftId=PlayerPrefs.GetString("watch_left",watchLeftId);watchRightId=PlayerPrefs.GetString("watch_right","typesafe:"+TypeSafeModel);}
 void SaveChoices(){PlayerPrefs.SetInt("setup_mode",matchup.index);PlayerPrefs.SetInt("setup_layout",layout.index);PlayerPrefs.SetInt("setup_length",matchLength.index);PlayerPrefs.SetInt("setup_timed",timed.value?1:0);PlayerPrefs.SetString("typesafe_model",TypeSafeModel);PlayerPrefs.SetString("laya_model",LayaModel);PlayerPrefs.SetString("watch_left",watchLeftId);PlayerPrefs.SetString("watch_right",watchRightId);PlayerPrefs.Save();}
 string SavedKey(){if(!savedLoaded){savedLoaded=true;savedKey=CredentialStore.Load();}return savedKey;}
 Seat SelectedLeft=>matchup.index==2?Seat.Parse(watchLeftId,Seat.Laya(LayaModel)):default;
 Seat SelectedRight=>matchup.index==0?Seat.Laya(LayaModel):matchup.index==1?Seat.TypeSafe(models.value):Seat.Parse(watchRightId,Seat.TypeSafe(TypeSafeModel));
 IEnumerable<Seat> SelectedSeats=>matchup.index==2?new[]{SelectedLeft,SelectedRight}:new[]{SelectedRight};
 bool UsesCloud=>SelectedSeats.Any(s=>s.Provider==AIProvider.TypeSafe);
 bool Verified=>verifiedKey!=null&&verifiedKey==key.value.Trim()&&verifiedModels!=null;
 LocalModel LocalEntry(string id)=>Local?.models?.FirstOrDefault(m=>m.name==id);
 // Reading the Keychain only when a cloud model is selected avoids prompts for local play.
 void FillKey(){if(!UsesCloud||!string.IsNullOrEmpty(key.value))return;var value=sessionKey??Environment.GetEnvironmentVariable("TYPESAFE_API_KEY")??(remember.value?SavedKey():null);if(!string.IsNullOrEmpty(value))key.SetValueWithoutNotify(value);}
 void AutoConnect(){if(Open&&UsesCloud&&!checking&&!string.IsNullOrWhiteSpace(key.value)&&verifiedKey!=key.value.Trim())StartCoroutine(CheckKey());}
 void SeatChanged(){FillKey();Warm();Refresh();AutoConnect();}
 void SetRemember(bool value){PlayerPrefs.SetInt("remember_key",value?1:0);if(value){if(verifiedKey!=null&&CredentialStore.Save(verifiedKey))savedKey=verifiedKey;}else{CredentialStore.Delete();savedKey=null;savedLoaded=true;}Refresh();}
 public void Forget(){CredentialStore.Delete();if(key.value.Trim()==savedKey)key.value="";savedKey=null;savedLoaded=true;Refresh();}
 // Polls the local catalog while setup is open so installs, downloads and loading show live.
 IEnumerator PollLocal(){while(true){if(Open&&matchup.index!=1){bool first=Local==null;LocalModelList list=null;string error=null;yield return LayaClient.LocalModels((l,e)=>{list=l;error=e;});Local=list;localError=error;RebuildChoices();Refresh();if(first&&list!=null)Warm();}yield return new WaitForSecondsRealtime(Local?.models!=null&&Local.models.Any(m=>m.downloading||m.loading)?.5f:1.5f);}}
 // Ask the service to load and warm the chosen local models before the first round.
 void Warm(){if(!Open||Local==null)return;foreach(var seat in SelectedSeats.Where(s=>s.Provider==AIProvider.Laya).Distinct()){var entry=LocalEntry(seat.Model);if(entry!=null&&entry.installed)StartCoroutine(LayaClient.LocalCommand(seat.Model,"load"));}}
 void Download(){if(downloadTarget==null)return;var entry=LocalEntry(downloadTarget);if(entry!=null)entry.downloading=true;StartCoroutine(LayaClient.LocalCommand(downloadTarget,"download",error=>{if(error!=null)Set(message,error);}));Refresh();}
 static string LocalLabel(LocalModel m)=>m.installed?m.label:$"{m.label}  ·  {m.size_mb} MB download";
 static void SetChoices(DropdownField field,List<string> choices,int index){if(!field.choices.SequenceEqual(choices))field.choices=choices;if(index>=0&&index<choices.Count&&field.value!=choices[index])field.SetValueWithoutNotify(choices[index]);}
 void RebuildChoices(){
  localIds.Clear();var labels=new List<string>();
  if(Local?.models!=null)foreach(var m in Local.models){localIds.Add(m.name);labels.Add(LocalLabel(m));}
  if(!localIds.Contains(LayaModel)){if(Local?.models!=null&&localIds.Count>0)LayaModel=localIds.Contains(Local.@default)?Local.@default:localIds[0];else{localIds.Add(LayaModel);labels.Add(LayaModel);}}
  SetChoices(layaModel,labels,localIds.IndexOf(LayaModel));
  seatIds.Clear();var seatLabels=new List<string>();
  for(int i=0;i<localIds.Count;i++){seatIds.Add("laya:"+localIds[i]);seatLabels.Add("Laya  ·  "+labels[i]);}
  foreach(var model in Verified?verifiedModels:new[]{TypeSafeModel}){if(seatIds.Contains("typesafe:"+model))continue;seatIds.Add("typesafe:"+model);seatLabels.Add("TypeSafe  ·  "+model+(Verified?"":"  ·  connect key"));}
  foreach(var id in new[]{watchLeftId,watchRightId})if(!seatIds.Contains(id)&&id.StartsWith("typesafe:")&&!Verified){seatIds.Add(id);seatLabels.Add("TypeSafe  ·  "+id.Substring(9)+"  ·  connect key");}
  if(!seatIds.Contains(watchLeftId))watchLeftId=seatIds[0];if(!seatIds.Contains(watchRightId))watchRightId=seatIds.FirstOrDefault(s=>s.StartsWith("typesafe:"))??seatIds[0];
  SetChoices(watchLeft,seatLabels,seatIds.IndexOf(watchLeftId));SetChoices(watchRight,new List<string>(seatLabels),seatIds.IndexOf(watchRightId));
 }
 // Why a seat cannot start yet, or null when it is ready. An unknown local catalog is allowed:
 // the game starts the local service on demand and waits without substituting another player.
 string Problem(Seat seat){
  if(seat.Provider==AIProvider.Laya){if(Local==null)return null;var m=LocalEntry(seat.Model);if(m==null)return seat.Model+" is not in the local model catalog";if(m.downloading)return $"Downloading {m.label}…";if(!m.installed)return $"Download {m.label} to play";if(!string.IsNullOrEmpty(m.error))return m.error;return null;}
  if(checking)return "Checking TypeSafe access…";if(!Verified)return string.IsNullOrWhiteSpace(key.value)?"Enter a TypeSafe key and connect before starting.":"Connect TypeSafe to use "+seat.Model;
  return Array.IndexOf(verifiedModels,seat.Model)<0?seat.Model+" is not available on this TypeSafe account":null;
 }
 static string LocalState(LocalModel m)=>!string.IsNullOrEmpty(m.download_error)?"Download failed · "+m.download_error:m.downloading?$"Downloading · {Mathf.RoundToInt(100*m.download_progress)}% of {m.size_mb} MB":!m.installed?$"Not installed · {m.size_mb} MB download":!string.IsNullOrEmpty(m.error)?m.error:m.loaded?"Ready on this Mac":m.loading?"Loading into memory…":"Installed";
 void RefreshLocal(){
  bool watch=matchup.index==2;Visible(layaModel,!watch);Visible(watchLeft,watch);Visible(watchRight,watch);Set(localTitle,watch?"PLAYERS  /  LOCAL OR CLOUD":"LOCAL MODEL  /  ON THIS MAC");
  var shown=SelectedSeats.Where(s=>s.Provider==AIProvider.Laya).Select(s=>s.Model).Distinct().ToArray();downloadTarget=null;
  if(Local==null)Set(localInfo,shown.Length==0?"":localError??"Connecting to local Laya…");
  else{var lines=new List<string>();foreach(var id in shown){var m=LocalEntry(id);if(m==null)continue;lines.Add($"{m.label}: {LocalState(m)}{(m.verified?" · verified":"")}\n{m.description}");if(!m.installed&&!m.downloading&&downloadTarget==null)downloadTarget=id;}Set(localInfo,string.Join("\n",lines));}
  var target=downloadTarget==null?null:LocalEntry(downloadTarget);Visible(download,target!=null);if(target!=null)download.text=$"Download {target.label}  ·  {target.size_mb} MB";
 }
 void RefreshSelections(){for(int i=0;i<modeButtons.Count;i++)modeButtons[i].EnableInClassList("selected",matchup.index==i);for(int i=0;i<arenaButtons.Count;i++)arenaButtons[i].EnableInClassList("selected",layout.index==i);for(int i=0;i<lengthButtons.Count;i++)lengthButtons[i].EnableInClassList("selected",matchLength.index==i);if(preview!=null){preview.Layout=layout.index;Set(arenaCaption,"THE ROOFTOP / "+layout.value.ToUpperInvariant());}}
 public void Show(){if(root==null)return;var a=Arena.Instance;if(a.Tutorial.Running)a.Tutorial.Stop();a.Research.Close(false);a.GetComponent<ExecutorDebugPanel>().Open=false;a.Match.Practice=false;Open=true;a.HUD.Pause(true);a.Match.Generation++;a.Client.CancelPending();a.LeftClient.CancelPending();a.StopExecutors("match_setup");if(Started)matchup.SetValueWithoutNotify(Modes[(int)Mode]);key.SetValueWithoutNotify("");FillKey();Visible(root,true);Refresh();AutoConnect();Warm();}
 void Refresh(){if(start==null)return;RefreshSelections();bool watch=matchup.index==2;Visible(localBox,matchup.index!=1);Visible(cloud,UsesCloud);Visible(models,matchup.index==1);Visible(back,Started);RefreshLocal();
  Set(versus,watch?$"{Title(SelectedLeft)}    ×    {Title(SelectedRight)}":matchup.index==1?"YOU    ×    TYPESAFE":"YOU    ×    LAYA");
  Set(explanation,watch?"Watch two independent AI players compete under the same rules. Pick any local Laya model or TypeSafe model for each corner; local models share this Mac's GPU.":matchup.index==1?"Take the cyan corner against TypeSafe's cloud model.":"Take the cyan corner against Laya, running locally on this Mac.");
  string problem=SelectedSeats.Select(Problem).FirstOrDefault(p=>p!=null);start.SetEnabled(!checking&&problem==null);check.SetEnabled(!checking&&!string.IsNullOrWhiteSpace(key.value));
  Visible(keyRow,CredentialStore.Persistent);Visible(forget,!string.IsNullOrEmpty(savedKey));Set(keyNote,!CredentialStore.Persistent||!remember.value?"Your key stays in this session only.":savedKey!=null&&savedKey==key.value.Trim()?"Saved in your macOS Keychain. Remove it any time with Forget saved key.":"After it connects, the key is saved in your macOS Keychain.");
  Set(message,problem??(UsesCloud?"Connected · "+string.Join(" + ",SelectedSeats.Where(s=>s.Provider==AIProvider.TypeSafe).Select(s=>s.Model).Distinct())+" · ready to play":"ENTER / Start    ·    WASD / Move    ·    F / Push or throw"));
 }
 static string Title(Seat seat)=>(seat.ProviderName+" · "+seat.Model).ToUpperInvariant();
 public IEnumerator CheckKey(){checking=true;verifiedKey=null;verifiedModels=null;int version=validationGeneration;string submitted=key.value.Trim();Refresh();string failure=null;yield return LayaClient.DiscoverModels(submitted,(names,error)=>{if(version!=validationGeneration)return;if(error!=null){failure=error;return;}verifiedKey=submitted;if(remember.value&&CredentialStore.Persistent&&savedKey!=submitted&&CredentialStore.Save(submitted))savedKey=submitted;key.SetValueWithoutNotify(submitted);verifiedModels=names;models.choices=new List<string>(names);models.SetValueWithoutNotify(Array.IndexOf(names,TypeSafeModel)>=0?TypeSafeModel:names[0]);},DiscoveryEndpoint);checking=false;if(version==validationGeneration){RebuildChoices();Refresh();if(failure!=null)Set(message,failure);}}
 public void StartFromKeyboard(){StartSelected();}
 void StartSelected(){if(!Open||!start.enabledSelf)return;var mode=(MatchMode)matchup.index;if(UsesCloud&&!Verified)return;if(!Configure(mode,SelectedLeft,SelectedRight,UsesCloud?key.value.Trim():null))return;Started=true;SaveChoices();Open=false;Visible(root,false);Arena.Instance.Match.Layout=layout.index;Arena.Instance.Match.TimedRounds=timed.value;Arena.Instance.Match.WinsRequired=new[]{3,5,7}[matchLength.index];Arena.Instance.Match.NewMatch();Arena.Instance.HUD.Pause(false);key.SetValueWithoutNotify("");}
 public void StartTutorial(){Configure(MatchMode.HumanVsLaya,default,Seat.Laya(LayaModel),sessionKey);Started=true;Open=false;Visible(root,false);Arena.Instance.Tutorial.Begin();}
 // Compatibility form: Human vs Laya uses the selected local model; cloud modes use the given TypeSafe model.
 public bool Configure(MatchMode mode,string model="jev-latest",string credential=null)=>Configure(mode,mode==MatchMode.AIVsAI?Seat.Laya(LayaModel):default,mode==MatchMode.HumanVsLaya?Seat.Laya(LayaModel):Seat.TypeSafe(model),credential);
 public bool Configure(MatchMode mode,Seat left,Seat right,string credential=null){
  if(!Enum.IsDefined(typeof(MatchMode),mode)||mode==MatchMode.HumanVsLaya&&right.Provider!=AIProvider.Laya||mode==MatchMode.HumanVsTypeSafe&&right.Provider!=AIProvider.TypeSafe)return false;
  var seats=mode==MatchMode.AIVsAI?new[]{left,right}:new[]{right};if(seats.Any(s=>string.IsNullOrWhiteSpace(s.Model)||s.Provider==AIProvider.TypeSafe&&string.IsNullOrWhiteSpace(credential)))return false;
  var a=Arena.Instance;a.Match.Generation++;a.StopExecutors("matchup_changed");a.Client.CancelPending();a.LeftClient.CancelPending();
  Mode=mode;LeftSeat=mode==MatchMode.AIVsAI?left:default;RightSeat=right;sessionKey=credential;foreach(var s in seats)if(s.Provider==AIProvider.TypeSafe)TypeSafeModel=s.Model;if(mode==MatchMode.HumanVsLaya)LayaModel=right.Model;
  if(mode==MatchMode.AIVsAI){string l=left.ProviderName,r=right.ProviderName;if(l==r){l+=" "+left.Model;r+=" "+right.Model;}if(l==r){l+=" (cyan)";r+=" (orange)";}LeftName=l;RightName=r;}else{LeftName="Human";RightName=right.ProviderName;}
  a.Human.Actor=a.Human.name=LeftName;a.AI.Actor=a.AI.name=RightName;
  if(mode==MatchMode.AIVsAI)a.LeftClient.Configure(left.Provider,left.Model,left.Provider==AIProvider.TypeSafe?credential:null);else a.LeftClient.Configure(AIProvider.Laya,LayaModel);
  a.Client.Configure(right.Provider,right.Model,right.Provider==AIProvider.TypeSafe?credential:null);a.LeftBrain.Enabled=mode==MatchMode.AIVsAI;a.Brain.Enabled=true;return true;
 }
 public void Close(){if(!Started)return;validationGeneration++;Open=false;Visible(root,false);key.SetValueWithoutNotify("");Arena.Instance.HUD.Pause(false);}
}
}
