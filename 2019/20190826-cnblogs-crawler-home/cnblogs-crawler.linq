<Query Kind="Statements">
  <NuGetReference>AngleSharp</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>AngleSharp</Namespace>
  <Namespace>AngleSharp.Dom</Namespace>
  <Namespace>AngleSharp.Html.Dom</Namespace>
  <Namespace>AngleSharp.Html.Parser</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
</Query>

var http = new HttpClient();
var parser = new HtmlParser();

File.WriteAllText(@"C:\Users\sdfly\Desktop\cnblogs.json", JsonConvert.SerializeObject(Enumerable.Range(1, 200)
	.AsParallel()
	.AsOrdered()
	.SelectMany(page =>
	{
		return Task.Run(async() =>
		{
			string pageData = await http.GetStringAsync($"https://www.cnblogs.com/sitehome/p/{page}".Dump());
			IHtmlDocument doc = await parser.ParseDocumentAsync(pageData);
			return doc.QuerySelectorAll(".post_item").Select(tag => new 
			{
				Title = tag.QuerySelector(".titlelnk").TextContent, 
				Page = page, 
				UserName = tag.QuerySelector(".post_item_foot .lightblue").TextContent, 
				PublishTime = DateTime.Parse(Regex.Match(tag.QuerySelector(".post_item_foot").ChildNodes[2].TextContent, @"(\d{4}\-\d{2}\-\d{2}\s\d{2}:\d{2})", RegexOptions.None).Value), 
				CommentCount = int.Parse(tag.QuerySelector(".post_item_foot .article_comment").TextContent.Trim()[3..^1]), 
				ViewCount = int.Parse(tag.QuerySelector(".post_item_foot .article_view").TextContent[3..^1]), 
				BriefContent = tag.QuerySelector(".post_item_summary").TextContent.Trim(), 
			});
		}).GetAwaiter().GetResult();
	}), Newtonsoft.Json.Formatting.Indented));