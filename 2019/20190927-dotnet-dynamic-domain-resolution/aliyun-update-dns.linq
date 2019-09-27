<Query Kind="Statements">
  <NuGetReference>aliyun-net-sdk-alidns</NuGetReference>
  <Namespace>Aliyun.Acs.Core</Namespace>
  <Namespace>Aliyun.Acs.Core.Profile</Namespace>
  <Namespace>Aliyun.Acs.Alidns.Model.V20150109</Namespace>
  <Namespace>System.Net</Namespace>
</Query>

string currentIp = new WebClient().DownloadString("https://echo-ip.starworks.cc/");

var client = new DefaultAcsClient(DefaultProfile.GetProfile("", Util.GetPassword("aliyun_dns_access_key"), Util.GetPassword("aliyun_dns_secret_key")));
var domainRecords = client.GetAcsResponse(new DescribeDomainRecordsRequest 
{ 
    DomainName = "starworks.cc", 
	RRKeyWord = "home", 
}).DomainRecords;
domainRecords.Dump();

DescribeDomainRecordsResponse.DescribeDomainRecords_Record homeRecord = domainRecords.First(x => x.RR == "home");

if (homeRecord._Value != currentIp)
{
	client.GetAcsResponse(new UpdateDomainRecordRequest
	{
		RecordId = homeRecord.RecordId,
		RR = homeRecord.RR,
		Type = homeRecord.Type,
		_Value = currentIp,
	});
	Util.Metatext($"{homeRecord._Value} -> {currentIp}").Dump();
}
else
{
	Util.Metatext("DNS not changed.").Dump();
}