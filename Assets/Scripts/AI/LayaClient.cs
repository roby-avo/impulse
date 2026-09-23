using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
namespace Playground {
[Serializable] public class ChoiceAnswer {public string choice;public float confidence;}
[Serializable] public class Answers {public ChoiceAnswer action;}
[Serializable] public class LayaResponse {public string model;public Answers answers;}
public class DecisionResult {public bool Valid;public SemanticAction Action;public float Confidence,LatencyMs;public string Error,Raw,RequestedAt,RespondedAt;}
public class LayaClient:MonoBehaviour {
 public string Endpoint="http://127.0.0.1:8000";public int TimeoutSeconds=8;public bool Busy;public string Status="Waiting for local Laya";public float LastLatency;public int ValidResponses,Failures;QuestionFile questionFile;
 void Awake(){questionFile=JsonUtility.FromJson<QuestionFile>(File.ReadAllText(Path.Combine(Application.streamingAssetsPath,"laya-question.json")));}
 public IEnumerator Decide(StateSnapshot state,Action<DecisionResult> complete,string[] allowed=null,string stateJson=null,string questionsJson=null){allowed=allowed??Enum.GetNames(typeof(SemanticAction));string questions=questionsJson??ActionSchema.Build(questionFile,allowed);if(Busy){complete(new DecisionResult{Error="request_already_pending"});yield break;}Busy=true;Status="Laya is deciding…";var started=Time.realtimeSinceStartup;var result=new DecisionResult{RequestedAt=DateTime.UtcNow.ToString("O")};
  using(var request=new UnityWebRequest(Endpoint+"/v1/systemone","POST")){request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes("{\"model\":\"english\",\"state\":"+(stateJson??JsonUtility.ToJson(state))+",\"questions\":"+questions+"}"));request.downloadHandler=new DownloadHandlerBuffer();request.SetRequestHeader("Content-Type","application/json");request.timeout=TimeoutSeconds;
   yield return request.SendWebRequest();result.LatencyMs=(Time.realtimeSinceStartup-started)*1000;result.RespondedAt=DateTime.UtcNow.ToString("O");result.Raw=request.downloadHandler.text;
   if(request.result!=UnityWebRequest.Result.Success)result.Error=request.error;else {try{var response=JsonUtility.FromJson<LayaResponse>(result.Raw);var choice=response?.answers?.action;if(choice!=null&&Enum.TryParse(choice.choice,out SemanticAction action)&&Enum.IsDefined(typeof(SemanticAction),action)&&choice.choice==action.ToString()&&Array.IndexOf(allowed,choice.choice)>=0&&!float.IsNaN(choice.confidence)&&choice.confidence>=0&&choice.confidence<=1){result.Valid=true;result.Action=action;result.Confidence=choice.confidence;}else result.Error="invalid_model_choice";}catch(Exception ex){result.Error="invalid_response: "+ex.Message;}}
  }
  Busy=false;LastLatency=result.LatencyMs;if(result.Valid){ValidResponses++;Status=result.Action+" · "+result.LatencyMs.ToString("0")+" ms";}else{Failures++;Status="Laya unavailable · neutral wait · retrying";Debug.LogWarning("Laya request failed: "+result.Error);}complete(result);
 }
}
}
