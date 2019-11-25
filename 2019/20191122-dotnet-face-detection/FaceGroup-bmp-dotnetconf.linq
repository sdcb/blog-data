<Query Kind="Program">
  <NuGetReference Prerelease="true">Microsoft.Azure.CognitiveServices.Vision.Face</NuGetReference>
  <NuGetReference>System.Interactive</NuGetReference>
  <Namespace>Microsoft.Azure.CognitiveServices.Vision.Face</Namespace>
  <Namespace>Microsoft.Azure.CognitiveServices.Vision.Face.Models</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Drawing</Namespace>
</Query>

#load ".\bitmap-ops.linq"
static string inFolder = @"D:\BaiduYunDownload\照片";
static string outFolder = inFolder + @"\out";
static DumpContainer dc = new DumpContainer().Dump("Status");

async Task Main()
{
	using var fc = new FaceClient(new ApiKeyServiceClientCredentials(Util.GetPassword("Face_Free")))
	{
		Endpoint = "https://southeastasia.api.cognitive.microsoft.com",
	};
	
    Directory.CreateDirectory(outFolder);
	var cache = new Cache<List<DetectedFace>>();
	int count = GetFiles(inFolder).Count();
	Dictionary<Guid, (string file, DetectedFace face)> faces = GetFiles(inFolder)
		.Select((file, i) => (file: file, faces: cache.GetOrCreate(file, () =>
		{
			try
			{
				byte[] bytes = CompressImage(file);
				var result = (file, faces: fc.Face.DetectWithStreamAsync(new MemoryStream(bytes)).GetAwaiter().GetResult());
				dc.Content = (result.faces.Count == 0 ? $"{file} not detect any face!!!" : $"{file}/{i}/{count} detected {result.faces.Count}.");
				return result.faces.ToList();
			}
			catch (OutOfMemoryException ex) {return null;}
			catch (Exception ex)
			{
				(ex.Message + ": " + file).Dump();
				return null;
			}
		})))
		.Where(x => x.faces != null)
		.SelectMany(x => x.faces.Select(face => (x.file, face)))
		.ToDictionary(x => x.face.FaceId.Value, x => (file: x.file, face: x.face));
	
	foreach (var buffer in faces.Buffer(1000).Select((list, groupId) => (list, groupId))
	{
		GroupResult group = await fc.Face.GroupAsync(buffer.list.Select(x => x.Key).ToList());
		
		var folder = outFolder + @"\gid-" + buffer.groupId;
		File.WriteAllText(@"D:\BaiduYunDownload\照片\out\gp-" + buffer.groupId + ".json", JsonSerializer.Serialize(group));
		CopyGroupAndDrawRect(folder, group, faces);
	}
}

class Cache<T>
{
	static string cacheFile = outFolder + @$"\cache-{typeof(T).Name}.json";
	Dictionary<string, T> cachingData;

	public Cache()
	{
		cachingData = File.Exists(cacheFile) switch
		{
			true => JsonSerializer.Deserialize<Dictionary<string, T>>(File.ReadAllBytes(cacheFile)),
			_ => new Dictionary<string, T>()
		};
	}

	public T GetOrCreate(string key, Func<T> fetchMethod)
	{
		if (cachingData.TryGetValue(key, out T cachedValue) && cachedValue != null)
		{
			return cachedValue;
		}

		var realValue = fetchMethod();
		
		lock(this)
		{
			cachingData[key] = realValue;
			File.WriteAllBytes(cacheFile, JsonSerializer.SerializeToUtf8Bytes(cachingData, new JsonSerializerOptions
			{
				WriteIndented = true, 
			}));
			return realValue;
		}
	}
}


void CopyGroupAndDrawRect(string outputPath, GroupResult result, Dictionary<Guid, (string file, DetectedFace face)> faces)
{
	foreach (var item in result.Groups
		.SelectMany((group, index) => group.Select(v => (faceId: v, index)))
		.Select(x => (info: faces[x.faceId], i: x.index + 1)))
	{
		string dir = Path.Combine(outputPath, item.i.ToString());
		Directory.CreateDirectory(dir);
		using var bmp = Bitmap.FromFile(item.info.file);
		HandleOrientation(bmp, bmp.PropertyItems);
		using (var g = Graphics.FromImage(bmp))
		{
			using var brush = new SolidBrush(Color.Red);
			using var pen = new Pen(brush, 5.0f);
			var rect = item.info.face.FaceRectangle;
			float scale = Math.Max(1.0f, (float)(1.0 * Math.Max(bmp.Width, bmp.Height) / 1920.0));
			g.ScaleTransform(scale, scale);
			g.DrawRectangle(pen, new Rectangle(rect.Left, rect.Top, rect.Width, rect.Height));
		}
		bmp.Save(Path.Combine(dir, Path.GetFileName(item.info.file)));
	}

	string messyFolder = Path.Combine(outputPath, "messy");
	Directory.CreateDirectory(messyFolder);
	foreach (var x in result.MessyGroup.Select(x => (file: faces[x].file, face: faces[x].face)))
	{
		using var bmp = Bitmap.FromFile(x.file);
		HandleOrientation(bmp, bmp.PropertyItems);
		using (var g = Graphics.FromImage(bmp))
		{
			using var brush = new SolidBrush(Color.Red);
			using var pen = new Pen(brush);
			var rect = x.face.FaceRectangle;
			float scale = Math.Max(1.0f, (float)(1.0 * Math.Max(bmp.Width, bmp.Height) / 1920.0));
			g.ScaleTransform(scale, scale);
			g.DrawRectangle(pen, new Rectangle(rect.Left, rect.Top, rect.Width, rect.Height));
		}
		bmp.Save(Path.Combine(messyFolder, Path.GetFileName(x.file)));
	}
}

IEnumerable<string> GetFiles(string folder)
{
    return Directory.EnumerateFiles(folder, "*.jpg", SearchOption.TopDirectoryOnly);
}