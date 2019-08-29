<Query Kind="Program">
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Newtonsoft.Json</Namespace>
</Query>

void Main()
{
	var data = JsonConvert.DeserializeObject<List<CnblogsItem>>(File.ReadAllText(@"C:\Users\sdfly\Desktop\cnblogs.json"));
	Util.Chart(data
			.GroupBy(x => x.PublishTime.DayOfWeek)
			.Select(x => new { WeekDay = x.Key, ArticleCount = x.Count() })
			.OrderBy(x => x.WeekDay),
		x => x.WeekDay.ToString(),
		y => y.ArticleCount).Dump();
}

class CnblogsItem
{
	public string TItle { get; set; }
	public int Page { get; set; }
	public string UserName { get; set; }
	public DateTime PublishTime { get; set; }
	public int CommentCount { get; set; }
	public int ViewCount { get; set; }
	public string BriefContent { get; set; }
}