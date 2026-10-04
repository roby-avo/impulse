using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
namespace Playground {
public enum AIProvider { Laya, TypeSafe }
[Serializable] public class ChoiceAnswer {public string type,choice;public float confidence=float.NaN;}
[Serializable] public class Answers {public ChoiceAnswer action;}
[Serializable] public class LayaResponse {public string model;public Answers answers;}
[Serializable] public class ModelEntry {public string name;}
[Serializable] public class ModelList {public ModelEntry[] models;}
// One checkpoint from the local service catalog (GET /v1/models on 127.0.0.1).
[Serializable] public class LocalModel {public string name,label,family,runtime,description,source,error,download_error,download_phase;public int size_mb;public bool installed,verified,loaded,loading,downloading,local_path;public float download_progress;}
[Serializable] public class LocalModelList {public string @default;public LocalModel[] models;}
public class DecisionResult {public bool Valid;public SemanticAction Action;public float Confidence,LatencyMs,InferenceMs;public string Device;public string Error,Raw,RequestedAt,RespondedAt,Provider,Model,ResponseModel;}
// Shared System One transport. The original component name is retained for scene compatibility.
public class LayaClient:MonoBehaviour {
 public const string TypeSafeEndpoint="https://api.typesafe.ai",LocalEndpoint="http://127.0.0.1:8000";
 public string Endpoint="http://127.0.0.1:8000",Model="english";public AIProvider Provider;public int TimeoutSeconds=8;
 public float RequestAge=>Busy?Time.realtimeSinceStartup-requestStarted:0;public string WaitStatus=>Busy?$"Deciding · {RequestAge:0.0}s / {TimeoutSeconds}s":(LastRequestFailed&&Provider==AIProvider.Laya&&LocalLayaService.CanManage(Endpoint)&&localStartup?LocalLayaService.Message??Status:Status);
 public bool LastRequestFailed{get;private set;}public bool Busy{get;private set;}public bool Blocked{get;private set;}public string Status="Waiting for local Laya";public float LastLatency;public int ValidResponses,Failures;public string LastDevice{get;private set;}
 public string Backend=>Provider==AIProvider.Laya?(LastDevice=="mps"?"Apple GPU":LastDevice=="cpu"?"CPU":LastDevice!=null?LastDevice+" runtime":"local"):"cloud";
 public bool ReadyToRequest=>!Busy&&!Blocked&&Time.realtimeSinceStartup>=retryAt;
 // Local models report their family (Laya, Von, Kev, ...); the provider value stays "Laya" in recordings.
 public string Family="Laya";
 public string DisplayName=>Provider==AIProvider.Laya?Family:"TypeSafe";
 bool localStartup;string apiKey;QuestionFile questionFile;UnityWebRequest pending;int revision,consecutiveFailures;float retryAt,requestStarted;
 void Awake(){questionFile=JsonUtility.FromJson<QuestionFile>(File.ReadAllText(Path.Combine(Application.streamingAssetsPath,"laya-question.json")));}
 public void Configure(AIProvider provider,string model=null,string key=null){CancelPending();Provider=provider;Endpoint=provider==AIProvider.Laya?LocalEndpoint:TypeSafeEndpoint;Model=provider==AIProvider.Laya&&string.IsNullOrWhiteSpace(model)?"english":model;apiKey=key;localStartup=false;Blocked=false;LastRequestFailed=false;retryAt=0;consecutiveFailures=0;LastDevice=null;Status="Ready · "+DisplayName;}
 public void CancelPending(){revision++;if(pending!=null){Status="Request cancelled · waiting";pending.Abort();}}
 public static string SafeError(long code)=>code==401||code==403?"API key rejected · open Match setup":code==429?"Rate limited · waiting before retry":code==402?"Account quota exhausted · open Match setup":code>=400&&code<500?"Request rejected (HTTP "+code+") · check model in Match setup":"Service unavailable · neutral wait · retrying";
 public static DecisionResult Parse(string raw,string[] allowed,bool typed=false){var result=new DecisionResult();try{var response=JsonUtility.FromJson<LayaResponse>(raw);var c=response?.answers?.action;if(c!=null&&(!typed||c.type=="choice")&&Enum.TryParse(c.choice,out SemanticAction action)&&Enum.IsDefined(typeof(SemanticAction),action)&&c.choice==action.ToString()&&Array.IndexOf(allowed,c.choice)>=0&&!float.IsNaN(c.confidence)&&!float.IsInfinity(c.confidence)&&c.confidence>=0&&c.confidence<=1){result.Valid=true;result.Action=action;result.Confidence=c.confidence;result.ResponseModel=response.model;}else result.Error="invalid_model_choice";}catch{result.Error="invalid_response";}return result;}
 public IEnumerator Decide(StateSnapshot state,Action<DecisionResult> complete,string[] allowed=null,string stateJson=null,string questionsJson=null){
  if(Busy){complete(new DecisionResult{Error="request_already_pending"});yield break;}
  if(Provider==AIProvider.TypeSafe&&string.IsNullOrWhiteSpace(apiKey)){Blocked=true;Status="API key required · open Match setup";complete(new DecisionResult{Error="api_key_required"});yield break;}
  allowed=allowed??Enum.GetNames(typeof(SemanticAction));string questions=questionsJson??ActionSchema.Build(questionFile,allowed);
  Busy=true;Status=DisplayName+" is deciding…";int version=revision;float started=Time.realtimeSinceStartup;requestStarted=started;string secret=apiKey;
  var result=new DecisionResult{RequestedAt=DateTime.UtcNow.ToString("O"),Provider=Provider.ToString(),Model=Model};
  using(var request=new UnityWebRequest(Endpoint+"/v1/systemone","POST")){
   pending=request;request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes("{\"model\":"+StateSchema.Quote(Model)+",\"state\":"+(stateJson??JsonUtility.ToJson(state))+",\"questions\":"+questions+"}"));request.downloadHandler=new DownloadHandlerBuffer();request.SetRequestHeader("Content-Type","application/json");if(Provider==AIProvider.TypeSafe)request.SetRequestHeader("Authorization","Bearer "+secret);request.timeout=TimeoutSeconds;
   yield return request.SendWebRequest();result.LatencyMs=(Time.realtimeSinceStartup-started)*1000;result.RespondedAt=DateTime.UtcNow.ToString("O");result.Device=request.GetResponseHeader("X-Laya-Device");float.TryParse(request.GetResponseHeader("X-Laya-Inference-Ms"),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out result.InferenceMs);
   if(version!=revision)result.Error="cancelled";
   else if(request.result!=UnityWebRequest.Result.Success){result.Error=SafeError(request.responseCode);bool local=Provider==AIProvider.Laya,loading=local&&request.responseCode==503&&request.GetResponseHeader("X-Laya-Status")=="loading";if(loading)result.Error="Loading "+Model+" · reconnecting automatically";else if(local&&request.responseCode==404)result.Error=Model+" is not installed · open Match setup";else if(local&&request.responseCode==422)result.Error=Model+" could not run · see ai/.runtime/service.log";localStartup=Provider==AIProvider.Laya&&request.responseCode==0&&LocalLayaService.CanManage(Endpoint);if(localStartup)result.Error=LocalLayaService.RequestStart();Blocked=request.responseCode>=400&&request.responseCode<500&&request.responseCode!=429&&request.responseCode!=408;float delay=Mathf.Min(30,Mathf.Pow(2,Mathf.Min(++consecutiveFailures,5)));if(float.TryParse(request.GetResponseHeader("Retry-After"),out float retry)&&!float.IsNaN(retry))delay=Mathf.Max(delay,Mathf.Clamp(retry,0,120));// The local worker is shared by both AI players, so local overlap retries almost at once.
   if(loading){consecutiveFailures=0;delay=1;}else if(local&&request.responseCode==429)delay=.25f;retryAt=Time.realtimeSinceStartup+(localStartup?1:delay);}
   else {string raw=request.downloadHandler.text;var parsed=Parse(raw,allowed,Provider==AIProvider.TypeSafe);result.Valid=parsed.Valid;result.Action=parsed.Action;result.Confidence=parsed.Confidence;result.ResponseModel=parsed.ResponseModel;result.Error=parsed.Error;
    // Never retain headers or error bodies. Redact the credential even if a server echoes it.
    result.Raw=string.IsNullOrEmpty(secret)?raw:raw.Replace(secret,"[REDACTED]");if(!result.Valid)retryAt=Time.realtimeSinceStartup+2;
   }
   pending=null;
  }
  Busy=false;if(version==revision){LastRequestFailed=!result.Valid;LastLatency=result.LatencyMs;if(result.Device!=null)LastDevice=result.Device;if(result.Valid){localStartup=false;consecutiveFailures=0;ValidResponses++;Status=result.Action+" · "+result.LatencyMs.ToString("0")+" ms";}else{Failures++;Status=result.Error=="invalid_model_choice"||result.Error=="invalid_response"?"Invalid model answer · neutral wait":result.Error;}}
  complete(result);
 }
 public static IEnumerator DiscoverModels(string key,Action<string[],string> complete,string endpoint=TypeSafeEndpoint){
  if(string.IsNullOrWhiteSpace(key)){complete(null,"Enter your TypeSafe API key.");yield break;}
  using(var request=UnityWebRequest.Get(endpoint+"/v1/models")){request.SetRequestHeader("Authorization","Bearer "+key);request.timeout=10;yield return request.SendWebRequest();
   if(request.result!=UnityWebRequest.Result.Success){complete(null,SafeError(request.responseCode));yield break;}
   string[] names=null;try{var data=JsonUtility.FromJson<ModelList>(request.downloadHandler.text);if(data?.models!=null)names=Array.ConvertAll(data.models,x=>x.name);}catch{}
   if(names==null||names.Length==0||Array.Exists(names,string.IsNullOrWhiteSpace))complete(null,"No usable models returned by TypeSafe.");else complete(names,null);
  }
 }
 public static IEnumerator LocalModels(Action<LocalModelList,string> complete,string endpoint=LocalEndpoint){
  using(var request=UnityWebRequest.Get(endpoint+"/v1/models")){request.timeout=3;yield return request.SendWebRequest();
   if(request.result!=UnityWebRequest.Result.Success){complete(null,request.responseCode==0&&LocalLayaService.CanManage(endpoint)?LocalLayaService.RequestStart()??"Starting local Laya…":"Local models unavailable (HTTP "+request.responseCode+")");yield break;}
   LocalModelList list=null;try{list=JsonUtility.FromJson<LocalModelList>(request.downloadHandler.text);}catch{}
   if(list?.models==null)complete(null,"Local Laya is out of date · restart it with ai/start.sh");else complete(list,null);
  }
 }
 // verb is "load" (load and warm) or "download". Both are local-only and need no credentials.
 public static IEnumerator LocalCommand(string model,string verb,Action<string> complete=null,string endpoint=LocalEndpoint){
  using(var request=new UnityWebRequest(endpoint+"/v1/models/"+UnityWebRequest.EscapeURL(model)+"/"+verb,"POST")){request.downloadHandler=new DownloadHandlerBuffer();request.timeout=5;yield return request.SendWebRequest();
   complete?.Invoke(request.result==UnityWebRequest.Result.Success?null:verb=="download"?"Download could not start (HTTP "+request.responseCode+")":null);
  }
 }
 void OnDestroy(){CancelPending();}
}
}
