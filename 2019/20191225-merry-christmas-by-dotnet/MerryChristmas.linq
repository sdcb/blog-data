<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Net.Http.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Windows.Forms.dll</Reference>
  <NuGetReference>FlysEngine.Desktop</NuGetReference>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>FlysEngine.Desktop</Namespace>
  <Namespace>SharpDX.Direct2D1</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>FlysEngine.Managers</Namespace>
  <Namespace>SharpDX</Namespace>
  <Namespace>SharpDX.DXGI</Namespace>
  <Namespace>System.Runtime.InteropServices</Namespace>
  <Namespace>System.Globalization</Namespace>
  <Namespace>System.Windows.Forms</Namespace>
</Query>

static DumpContainer dc = new DumpContainer().Dump("Snow Count: ");

class MerryChristmas : LayeredRenderWindow
{
	readonly string[] _mcs = new[]
	{
		"https://gitee.com/sdcb/lovegl/raw/master/lovegl2014/design/mc1.png",
		"https://gitee.com/sdcb/lovegl/raw/master/lovegl2014/design/mc2.png",
		"https://gitee.com/sdcb/lovegl/raw/master/lovegl2014/design/mc3.png",
	}.Select(x => LoadUrlAsTempFile(x)).ToArray();

	int _mcIndex = 0;
	public string CurrentMc => _mcs[_mcIndex];

	readonly System.Windows.Forms.Timer _timer;

	public MerryChristmas()
	{
		TopMost = true; TopLevel = true;
		DragMoveEnabled = true; ShowInTaskbar = false; StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;

		_timer = new System.Windows.Forms.Timer() { Interval = 500, Enabled = true };
		_timer.Tick += (o, args) =>
		{
			_mcIndex = (_mcIndex + 1) % _mcs.Length;
		};
	}

	protected override void OnDraw(DeviceContext ctx)
	{
		ctx.Clear(Color.Transparent);
		ctx.DrawBitmap(XResource.Bitmaps[CurrentMc], 1.0f, InterpolationMode.Linear);
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (disposing)
		{
			_timer.Dispose();
		}
	}
}

class SnowWindow : LayeredRenderWindow
{
	System.Windows.Forms.Timer _timer;
	Dictionary<Guid, Snow> _snows = new Dictionary<System.Guid, UserQuery.SnowWindow.Snow>();
	MerryChristmas _mcWindow = new MerryChristmas();
	readonly string _musicPath;
	
	public SnowWindow()
	{
		WindowState = System.Windows.Forms.FormWindowState.Maximized;
		Text = "Merry Christmas!"; TopLevel = true; TopMost = true;
		_timer = new System.Windows.Forms.Timer() { Interval = 1000, Enabled = true, };
		_timer.Tick += (o, e) => CreateSnow();

		_musicPath = GetMusicPath();
		$"open music: {_musicPath}...".Dump();
		mciSendString($"open {_musicPath} type sequencer alias music", null, 0, Handle);
		
		PlayMusic();
		$"Window initialize complete!".Dump();
	}

	void PlayMusic()
	{
		$"play music: {_musicPath}...".Dump();
		mciSendString($"play music from 0 notify", null, 0, Handle);
	}

	protected override void WndProc(ref Message m)
	{
		base.WndProc(ref m);
		
		const int MM_MCINOTIFY = 953;
		if (m.Msg == MM_MCINOTIFY)
		{
			PlayMusic();
		}
	}

	void CreateSnow()
	{
		var snow = Snow.CreateRandom(this);
		_snows[snow.Id] = snow;
	}

	protected override void OnLoad(EventArgs e)
	{
		base.OnLoad(e);
		_mcWindow.Show(this);
	}

	public override void Render(int syncInterval, PresentFlags presentFlags)
	{
		base.Render(syncInterval, presentFlags);
		if (!_mcWindow.IsDisposed)
		{
			_mcWindow.Render(syncInterval, presentFlags);
		}
	}

	protected override void OnUpdateLogic(float lastFrameTimeInSecond)
	{
		foreach (var snow in _snows.Values)
		{
			snow.Update(RenderTimer);
		}
		
		var idsToDispose = _snows.Values
			.Where(x => x.IsOffScreen)
			.Select(x => x.Id).ToList();
			
		foreach (var id in idsToDispose) _snows.Remove(id);
		dc.Content = _snows.Count;
	}

	protected override void OnClosed(EventArgs e)
	{
		base.OnClosed(e);
		mciSendString("stop music notify", null, 0, Handle);
	}

	protected override void OnDraw(DeviceContext ctx)
	{
		ctx.Clear(Color.Transparent);
		foreach (var snow in _snows.Values)
		{
			snow.Draw(XResource.Bitmaps, ctx);
		}
		ctx.Transform = Matrix3x2.Identity;
	}

	class Snow
	{
		readonly static string _snow = LoadUrlAsTempFile("https://gitee.com/sdcb/lovegl/raw/master/lovegl2014/icon1.ico");
		
		public Guid Id { get; set; } = Guid.NewGuid();
		
		public float Direction { get; set; }

		public float RotationAngle { get; set;}

		public float Scale { get; set; }

		public float Speed { get; set; }

		public float AngularSpeed { get; set;}

		public float X { get; set;}
		
		public float Y { get; set;}
		
		public bool IsOffScreen 
			=> X < -50 || X > RenderWindow.Width + 50 || Y > RenderWindow.Height + 50;
		
		public RenderWindow RenderWindow { get; set; }
		
		public void Update(RenderTimer timer)
		{
			var dt = (float)timer.DurationSinceLastFrame.TotalSeconds;
			X += (float)(Speed * Math.Sin(Direction) * dt);
			Y += (float)(Speed * Math.Cos(Direction) * dt);
			RotationAngle += AngularSpeed * dt;
		}
		
		public void Draw(BitmapManager bmp, RenderTarget ctx)
		{
			ctx.Transform = 
				Matrix3x2.Translation(-24, -24) * 
				Matrix3x2.Rotation(RotationAngle) * 
				Matrix3x2.Scaling(Scale) * 
				Matrix3x2.Translation(X, Y);
			ctx.DrawBitmap(bmp[_snow], 1.0f, BitmapInterpolationMode.Linear);
		}
		
		static Random r = new Random();
		public static Snow CreateRandom(RenderWindow renderWindow)
		{
			return new Snow
			{
				Direction = r.NextFloat(-30, 30) * (float)Math.PI / 180, 
				Scale = r.NextFloat(0.5f, 1.2f), 
				Speed = r.NextFloat(80, 100), 
				AngularSpeed = r.NextFloat(-120, 120) * (float)Math.PI / 180, 
				X = r.NextFloat(50, renderWindow.Width - 50), 
				Y = -50, 
				RenderWindow = renderWindow, 
			};
		}
	}
}

static async Task<byte[]> LoadUrlAsTempFileAsync(string url)
{
	var response = await httpClient.GetAsync(url).ConfigureAwait(false);
	return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
}

static string LoadUrlAsTempFile(string url)
{
	$"Loading {url}...".Dump();
	var bytes = Util.Cache(() => LoadUrlAsTempFileAsync(url).Result, url);
	
	var path = Path.GetTempPath() + Path.GetFileName(url);
	File.WriteAllBytes(path, bytes);
	return path;
}

static string GetMusicPath()
{
	var url = "https://gitee.com/sdcb/lovegl/raw/master/lovegl2014/music.cpp";
	$"Loading {url}...".Dump();

	var musicBytes = Util.Cache(() =>
	{
		var resp = httpClient.GetAsync(url).Result;
		var content = resp.Content.ReadAsStringAsync().Result;
		return Regex.Matches(content, @"0x([\dA-F]{2})")
			.Cast<Match>()
			.Select(x => byte.Parse(x.Groups[1].Value, NumberStyles.HexNumber))
			.ToArray();
	}, url);
	
	var path = Path.GetTempPath() + "merry-christmas.mid";
	File.WriteAllBytes(path, musicBytes);
	return path;
}

void Main()
{
	Util.NewProcess = true;
	using (var window = new SnowWindow())
	{
		RenderLoop.Run(window,() => window.Render(1, 0));
	}
}

static HttpClient httpClient = new HttpClient();

[DllImport("winmm.dll")]
private static extern long mciSendString(string Cmd, StringBuilder StrReturn, int ReturnLength, IntPtr HwndCallback);