<Query Kind="Program">
  <NuGetReference>FlysEngine.Desktop</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>System.Reactive</NuGetReference>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Net.Sockets</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Reactive.Subjects</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>FlysEngine.Desktop</Namespace>
  <Namespace>SharpDX</Namespace>
  <Namespace>System.Windows.Forms</Namespace>
  <Namespace>System.Collections.Concurrent</Namespace>
  <Namespace>SharpDX.DXGI</Namespace>
  <Namespace>System.Reactive.Concurrency</Namespace>
  <Namespace>SharpDX.DirectWrite</Namespace>
  <Namespace>SharpDX.Direct2D1</Namespace>
</Query>

#load "new-barrage.linq"

void Main()
{
	const float FontSize = 35;
	var dc = new DumpContainer().Dump();
	var barrages = new LinkedList<OnScreenBarrage>();
	var form = new LayeredRenderWindow { WindowState = FormWindowState.Maximized, TopMost = true };
	IDisposable connection = null;
	form.Load += (o, args) =>
	{
		connection = DouyuBarrage.ChatMessageFromUrl("https://www.douyu.com/71415")
			.Subscribe(b =>
			{
				form.Invoke(new Action(() =>
				{
					var osb = new OnScreenBarrage
					{
						Position = new Vector2(form.XResource.RenderTarget.Size.Width - 1, GetNewY()),
						TextLayout = form.XResource.TextLayouts[b.Message, FontSize],
						Color = new Color((b.Color << 8) + 0xff), 
					};
					barrages.AddLast(osb);
				}));
			});
	};

	form.FormClosing += (o, e) => connection?.Dispose();

	form.UpdateLogic += (form, dt) =>
	{
		var node = barrages.First;
		while (node != null)
		{
			var next = node.Next;
			node.Value.MoveLeft(dt, form.Width / 19);
			if (!node.Value.IsOnScreen(form.XResource.RenderTarget.Size))
				barrages.Remove(node);
			node = next;
		}
	};

	form.Draw += (form, ctx) =>
	{
		ctx.Clear(Color.Transparent);
		foreach (var item in barrages)
		{
			ctx.DrawTextLayout(item.Position, item.TextLayout, form.XResource.GetColor(item.Color), 
				DrawTextOptions.EnableColorFont);
		}
	};

	float GetNewY()
	{
		float y = 0;
		while (barrages.Reverse().Where(x => x.Position.Y == y).Select(x => x.Rect.Right).FirstOrDefault() > form.Width)
		{
			y += FontSize;
		}
		return y;
	}

	QueryCancelToken.Register(() => form.Close());
	RenderLoop.Run(form, () => form.Render(1, PresentFlags.None));
}

class OnScreenBarrage
{
	public Vector2 Position;

	internal TextLayout TextLayout;
	
	public Color Color;

	public RectangleF Rect => new RectangleF(Position.X, Position.Y, TextLayout.Metrics.Width, TextLayout.Metrics.Height);

	public bool IsOnScreen(Size2F screenSize) => Rect.Right > 0;

	public void MoveLeft(float dt, float speed)
	{
		Position.X -= dt * speed;
	}
}