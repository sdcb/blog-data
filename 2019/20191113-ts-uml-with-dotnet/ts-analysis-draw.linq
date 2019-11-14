<Query Kind="Program">
  <NuGetReference>FlysEngine.Desktop</NuGetReference>
  <NuGetReference>Sdcb.TypeScriptAST</NuGetReference>
  <Namespace>FlysEngine.Desktop</Namespace>
  <Namespace>Sdcb.TypeScript</Namespace>
  <Namespace>Sdcb.TypeScript.TsTypes</Namespace>
  <Namespace>SharpDX</Namespace>
  <Namespace>SharpDX.Direct2D1</Namespace>
  <Namespace>SharpDX.DirectWrite</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Windows.Forms</Namespace>
</Query>

#load ".\ts-analysis.linq"

static DumpContainer dc = new DumpContainer().Dump();

void Main()
{
	using (var w = new Window
	{
		Size = new System.Drawing.Size(1024, 768),
		StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
	})
	{
		RenderLoop.Run(w, () => w.Render(1, SharpDX.DXGI.PresentFlags.None));
	}
}

class Window : RenderWindow
{
	Dictionary<string, RenderingClassDef> AllClass =
		ParseFiles(Directory.EnumerateFiles(path: @"C:\Users\sdfly\source\repos\ShootR\ShootR\ShootR\Client\Ships", "*.ts"))
		.ToDictionary(k => k.Key, v => new RenderingClassDef { Def = v.Value });
	const float FontSize = 12.0f;
	const float MarginLR = 5.0f;
	static Color TextColor = Color.Black;
	Matrix3x2 worldTransform = Matrix3x2.Identity;

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		float scale = MathF.Pow(1.1f, e.Delta / 120.0f);
		worldTransform *= Matrix3x2.Scaling(scale, scale, mousePos);
	}

	protected override void OnKeyUp(KeyEventArgs e)
	{
		if (e.KeyCode == Keys.Space) worldTransform = Matrix3x2.Identity;
	}

	protected override void OnDraw(DeviceContext ctx)
	{
		ctx.Clear(Color.White);
		ctx.Transform = worldTransform;

		foreach (var classDef in AllClass.Values)
		{
			List<string> allTypes = classDef.Def.Properties.Select(x => x.Type).ToList();
			foreach (var kv in AllClass.Where(x => allTypes.Contains(x.Key)))
			{
				ctx.DrawLine(classDef.Center, kv.Value.Center, XResource.GetColor(Color.Gray), 2.0f);
			}
		}

		foreach (var classDef in AllClass.Values.OrderBy(x => x.ZIndex))
		{
			ctx.FillRectangle(new RectangleF(classDef.Position.X, classDef.Position.Y, classDef.Size.X, classDef.Size.Y), XResource.GetColor(Color.AliceBlue));

			var position = classDef.Position;
			List<TextLayout> lines =
				classDef.Def.Properties.OrderByDescending(x => x.IsPublic).Select(x => x.ToString())
				.Concat(new string[] { "" })
				.Concat(classDef.Def.Methods.OrderByDescending(m => m.IsPublic).Select(x => x.ToString()))
				.Select(x => XResource.TextLayouts[x, FontSize])
				.ToList();

			TextLayout titleLayout = XResource.TextLayouts[classDef.Def.Name, FontSize + 3];
			float width = Math.Max(titleLayout.Metrics.Width, lines.Max(x => x.Metrics.Width)) + MarginLR * 2;
			ctx.DrawTextLayout(new Vector2(position.X + (width - titleLayout.DetermineMinWidth()) / 2 + MarginLR, position.Y), titleLayout, XResource.GetColor(Color.Black));
			ctx.DrawLine(new Vector2(position.X, position.Y + titleLayout.Metrics.Height), new Vector2(position.X + width, position.Y + titleLayout.Metrics.Height), XResource.GetColor(TextColor), 2.0f);

			float y = lines.Aggregate(position.Y + titleLayout.Metrics.Height, (y, pt) =>
			{
				if (pt.Metrics.Width == 0)
				{
					ctx.DrawLine(new Vector2(position.X, y), new Vector2(position.X + width, y), XResource.GetColor(TextColor), 2.0f);
					return y;
				}
				else
				{
					ctx.DrawTextLayout(new Vector2(position.X + MarginLR, y), pt, XResource.GetColor(TextColor));
					return y + pt.Metrics.Height;
				}
			});
			float height = y - position.Y;

			ctx.DrawRectangle(new RectangleF(position.X, position.Y, width, height), XResource.GetColor(TextColor), 2.0f);
			classDef.Size = new Vector2(width, height);

		}
	}

	Vector2 mousePos;
	protected override void OnMouseMove(MouseEventArgs e)
	{
		mousePos = XResource.InvertTransformPoint(worldTransform, new Vector2(e.X, e.Y));
		foreach (var item in this.AllClass.Values)
		{
			if (item.CapturedPosition != null)
			{
				item.Position = item.OriginPosition + mousePos - item.CapturedPosition.Value;
				return;
			}
		}
	}

	protected override void OnClick(EventArgs e)
	{
		foreach (var item in this.AllClass.Values)
		{
			if (item.TestPoint(mousePos))
			{
				item.ZIndex = this.AllClass.Values.Max(v => v.ZIndex) + 1;
			}
		}
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		foreach (var item in this.AllClass.Values)
		{
			item.CapturedPosition = null;
		}

		foreach (var item in this.AllClass.Values)
		{
			if (item.TestPoint(mousePos))
			{
				item.CapturedPosition = mousePos;
				item.OriginPosition = item.Position;
				return;
			}
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		foreach (var item in this.AllClass.Values)
		{
			item.CapturedPosition = null;
		}
	}
}

class RenderingClassDef
{
	public ClassDef Def { get; set; }

	public Vector2? CapturedPosition { get; set; }

	public Vector2 OriginPosition { get; set; }

	public Vector2 Position { get; set; }

	public Vector2 Size { get; set; }

	public Vector2 Center => Position + Size / 2;

	public int ZIndex { get; set; } = 0;

	public bool TestPoint(Vector2 point) => new RectangleF(Position.X, Position.Y, Size.X, Size.Y).Contains(point);
}