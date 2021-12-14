<Query Kind="Expression">
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>System.Reactive</NuGetReference>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
  <Namespace>System.Net.Sockets</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Reactive.Subjects</Namespace>
</Query>

#load "new-barrage.linq"

DouyuBarrage.ChatMessageFromUrl("https://www.douyu.com/71415")
	.Select(x => new  { Room = "scboy", Message = x.Message })
.Merge(DouyuBarrage.ChatMessageFromUrl("https://www.douyu.com/5110403")
	.Select(x => new { Room = "lalala", Message = x.Message}))