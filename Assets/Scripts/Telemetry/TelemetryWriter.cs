using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
namespace Playground {
// One ordered writer owns all streams. Unity objects never cross this boundary.
public sealed class TelemetryWriter:IDisposable {
 readonly BlockingCollection<Action> queue=new();readonly Dictionary<string,StreamWriter> streams=new();readonly Thread worker;public string Error{get;private set;}
 public TelemetryWriter(){worker=new Thread(Run){IsBackground=true,Name="Physics Playground telemetry"};worker.Start();}
 StreamWriter Stream(string path){if(!streams.TryGetValue(path,out var writer)){if(streams.Count>=8){foreach(var s in streams.Values)s.Dispose();streams.Clear();}writer=new StreamWriter(new FileStream(path,FileMode.Append,FileAccess.Write,FileShare.ReadWrite),new System.Text.UTF8Encoding(false),65536);streams.Add(path,writer);}return writer;}
 public void Write(string path,string json){queue.Add(()=>Stream(path).WriteLine(json));}
 public void Csv(string path,string header,string row){queue.Add(()=>{bool needsHeader=!File.Exists(path);var s=Stream(path);if(needsHeader)s.WriteLine(header);s.WriteLine(row);});}
 public Task FlushAsync(){var done=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);queue.Add(()=>{try{FlushStreams();if(Error!=null)done.TrySetException(new IOException(Error));else done.TrySetResult(true);}catch(Exception e){done.TrySetException(e);}});return done.Task;}
 public Task<string[]> SnapshotAsync(string path){var done=new TaskCompletionSource<string[]>(TaskCreationOptions.RunContinuationsAsynchronously);queue.Add(()=>{try{FlushStreams();if(Error!=null)throw new IOException(Error);done.TrySetResult(File.ReadAllLines(path));}catch(Exception e){done.TrySetException(e);}});return done.Task;}
 void FlushStreams(){foreach(var s in streams.Values)s.Flush();}
 void Run(){var timer=System.Diagnostics.Stopwatch.StartNew();try{while(!queue.IsCompleted){if(queue.TryTake(out var work,50)){try{work();}catch(Exception e){Error=e.Message;}}if(timer.ElapsedMilliseconds>=250){try{FlushStreams();}catch(Exception e){Error=e.Message;}timer.Restart();}}try{FlushStreams();}catch(Exception e){Error=e.Message;}}finally{foreach(var s in streams.Values)try{s.Dispose();}catch(Exception e){Error=e.Message;}}}
 public void Dispose(){queue.CompleteAdding();worker.Join();queue.Dispose();}
}
}
