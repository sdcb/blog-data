<Query Kind="Statements">
  <NuGetReference>aliyun-net-sdk-alidns</NuGetReference>
  <Namespace>Aliyun.Acs.Core</Namespace>
  <Namespace>Aliyun.Acs.Core.Profile</Namespace>
  <Namespace>Aliyun.Acs.Alidns.Model.V20150109</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>LINQPad.Controls</Namespace>
</Query>

var client = new DefaultAcsClient(DefaultProfile.GetProfile("", Util.GetPassword("aliyun_dns_access_key"), Util.GetPassword("aliyun_dns_secret_key")));
const string DomainName = "starworks.cc";
// create line
var rrBox = new TextBox { Width = "10em" };
rrBox.HtmlElement.SetAttribute("placeholder", "RR...");
var typeBox = new SelectBox(new[] { "A", "CNAME", "MX", "TXT", "SRV", "FORWARD_URL", "REDIRECT_URL"});
var valueBox = new TextBox(new WebClient().DownloadString("https://echo-ip.starworks.cc/"));
var updateButton = new Button("Add/Edit");
var refreshButton = new Button("Refresh");
Util.HorizontalRun(true, rrBox, typeBox, valueBox, updateButton, refreshButton).Dump();

var dc = new DumpContainer().Dump();
UpdateDC();

void UpdateDC()
{
	dc.Content = Util.Metatext("Loading...");
	var domainRecords = client.GetAcsResponse(new DescribeDomainRecordsRequest{DomainName = DomainName, PageSize = 100}).DomainRecords
		.Select(x => new
		{
			RR = x.RR,
			Type = x.Type,
			Value = x._Value,
			Edit = new Button("Edit", b => { rrBox.Text = x.RR; valueBox.Text = x._Value; typeBox.SelectedOption = x.Type; }),
            Delete = new Button("Delete", b => DeleteRecord(x))
		});
	dc.Content = domainRecords;
}

void DeleteRecord(DescribeDomainRecordsResponse.DescribeDomainRecords_Record r)
{
    dc.Content = client.GetAcsResponse(new DeleteDomainRecordRequest
    {
        RecordId = r.RecordId, 
    });
}

void AddRecord()
{
	var domainRecords = client.GetAcsResponse(new DescribeDomainRecordsRequest
	{
		DomainName = DomainName,
		RRKeyWord = rrBox.Text,
	}).DomainRecords;
	if (domainRecords.Count == 0)
	{
		dc.Content = client.GetAcsResponse(new AddDomainRecordRequest
		{
			DomainName = DomainName,
			RR = rrBox.Text,
			Type = (string)typeBox.SelectedOption,
			_Value = valueBox.Text,
		});
	}
	else
	{
		string recordId = domainRecords.First().RecordId;
		dc.Content = client.GetAcsResponse(new UpdateDomainRecordRequest
		{
			RecordId = recordId,
			RR = rrBox.Text, 
			Type = (string)typeBox.SelectedOption,
			_Value = valueBox.Text,
		});
	}
}

updateButton.Click += (o,e) => AddRecord();

refreshButton.Click += (o,e) => UpdateDC();