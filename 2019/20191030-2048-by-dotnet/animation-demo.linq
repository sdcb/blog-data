<Query Kind="Program">
  <NuGetReference>FlysEngine.Desktop</NuGetReference>
  <NuGetReference>System.Reactive</NuGetReference>
  <Namespace>FlysEngine.Desktop</Namespace>
  <Namespace>SharpDX</Namespace>
  <Namespace>SharpDX.Animation</Namespace>
  <Namespace>SharpDX.Direct2D1</Namespace>
  <Namespace>SharpDX.DXGI</Namespace>
  <Namespace>System.Reactive</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Windows.Forms</Namespace>
</Query>

void Main()
{
	using var g = new GameWindow();
	RenderLoop.Run(g, () => g.Render(1, PresentFlags.None));
}

public class GameWindow : RenderWindow
{
	public static GameWindow Instance = null;

	public static Task CreateAnimation(float initialVal, float finalVal, float durationMs, Action<float> setter)
	{
		var tcs = new TaskCompletionSource<float>();
		Variable variable = Instance.XResource.CreateAnimation(initialVal, finalVal, durationMs / 1000);

		IDisposable subscription = null;
		subscription = Observable
			.FromEventPattern<RenderWindow, float>(Instance, nameof(Instance.UpdateLogic))
			.Select(x => x.EventArgs)
			.Subscribe(x =>
			{
				setter((float)variable.Value);
				if (variable.FinalValue == variable.Value)
				{
					tcs.SetResult(finalVal);
					variable.Dispose();
					subscription.Dispose();
				}
			});

		return tcs.Task;
	}

	public GameWindow()
	{
		Instance = this;
		StartPosition = FormStartPosition.CenterScreen;
		ClientSize = new System.Drawing.Size(400, 400);
	}

	float x = 50, y = 150, w = 50, h = 50;
	float red = 0;
	protected override async void OnLoad(EventArgs e)
	{
		var stage1 = new[]
		{
			CreateAnimation(initialVal: x, finalVal: 340, durationMs: 1000, v => x = v),
			CreateAnimation(initialVal: h, finalVal: 100, durationMs: 600, v => h = v),
		};
		await Task.WhenAll(stage1);
		await CreateAnimation(initialVal: h, finalVal: 50, durationMs: 1000, v => h = v);
		await CreateAnimation(initialVal: x, finalVal: 20, durationMs: 1000, v => x = v);
		while (true)
		{
			await CreateAnimation(initialVal: red, finalVal: 1.0f, durationMs: 500, v => red = v);
			await CreateAnimation(initialVal: red, finalVal: 0.0f, durationMs: 500, v => red = v);
		}
	}

	protected override void OnDraw(DeviceContext ctx)
	{
		ctx.Clear(Color.CornflowerBlue);
		var displayColor = XResource.GetColor(new Color(red, 0.0f, 0.0f, 1.0f));
		ctx.FillRectangle(new RectangleF(x, y, w, h), displayColor);
		ctx.DrawText(
			$"(x,y) = ({x:N2},{y:N2})\n(w,h) = ({w:N2},{h:N2})\nred = {red:N2}", 
			XResource.TextFormats[20.0f], 
			new RectangleF(0, 0, 400, 400), 
			displayColor);
	}
}