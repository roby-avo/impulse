using System;
using System.Runtime.InteropServices;
using UnityEngine;
namespace Playground {
// Opt-in persistence for the TypeSafe key. macOS uses the login Keychain; it is never
// written to PlayerPrefs, profiles, telemetry or exports. Batch runs use memory only.
public static class CredentialStore {
 public const string Service="ai.impulse.typesafe",Account="api-key",Label="IMPULSE · TypeSafe API key";
 public interface IBackend {bool Persistent{get;}string Load();bool Save(string secret);bool Delete();}
 public class Memory:IBackend {string secret;public bool Persistent=>false;public string Load()=>secret;public bool Save(string value){secret=value;return true;}public bool Delete(){secret=null;return true;}}
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
 public class Keychain:IBackend {
  [DllImport("PlaygroundDock")] static extern int PlaygroundKeychainSave(string service,string account,string label,string secret);
  [DllImport("PlaygroundDock")] static extern IntPtr PlaygroundKeychainLoad(string service,string account,out int status);
  [DllImport("PlaygroundDock")] static extern int PlaygroundKeychainDelete(string service,string account);
  [DllImport("PlaygroundDock")] static extern void PlaygroundKeychainFree(IntPtr value);
  readonly string service,account;public int LastStatus{get;private set;}
  public Keychain(string service=Service,string account=Account){this.service=service;this.account=account;}
  public bool Persistent=>true;
  public string Load(){var pointer=PlaygroundKeychainLoad(service,account,out int status);LastStatus=status;if(pointer==IntPtr.Zero)return null;try{return Marshal.PtrToStringUTF8(pointer);}finally{PlaygroundKeychainFree(pointer);}}
  public bool Save(string secret){LastStatus=PlaygroundKeychainSave(service,account,Label,secret);return LastStatus==0;}
  public bool Delete(){LastStatus=PlaygroundKeychainDelete(service,account);return LastStatus==0;}
 }
#endif
 static IBackend backend;
 public static IBackend Backend{get=>backend??=Default();set=>backend=value;}
 static IBackend Default(){
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
  if(!Application.isBatchMode)return new Keychain();
#endif
  return new Memory();
 }
 public static bool Persistent=>Backend.Persistent;
 public static string Load(){try{var value=Backend.Load();return string.IsNullOrWhiteSpace(value)?null:value;}catch(Exception e){Debug.LogWarning("Saved key unavailable: "+e.GetType().Name);return null;}}
 public static bool Save(string secret){if(string.IsNullOrWhiteSpace(secret))return false;try{return Backend.Save(secret.Trim());}catch(Exception e){Debug.LogWarning("Could not save key: "+e.GetType().Name);return false;}}
 public static bool Delete(){try{return Backend.Delete();}catch(Exception e){Debug.LogWarning("Could not delete key: "+e.GetType().Name);return false;}}
}
}
