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

const int MatrixSize = 4;

static IEnumerable<int> inorder = Enumerable.Range(0, MatrixSize);
static IEnumerable<(int x, int y)> MatrixPositions =>
	inorder.SelectMany(y => inorder.Select(x => (x, y)));

void Main()
{
	using var g = new GameWindow();
	RenderLoop.Run(g, () => g.Render(1, PresentFlags.None));
}

public class GameWindow : RenderWindow
{
	Matrix Matrix = new Matrix();

	public GameWindow()
	{
		ClientSize = new System.Drawing.Size(400, 400);
	}

	protected override void OnLoad(EventArgs e)
	{
		Matrix.ReInitialize();
		Text = $"总分：{Matrix.GetScore()}";
	}

	protected override void OnDraw(DeviceContext ctx)
	{
		ctx.Clear(new Color(0xffa0adbb));

		float fullEdge = Math.Min(ctx.Size.Width, ctx.Size.Height);
		float gap = fullEdge / (MatrixSize * 8);
		float edge = (fullEdge - gap * (MatrixSize + 1)) / MatrixSize;

		foreach (var v in MatrixPositions)
		{
			float centerX = gap + v.x * (edge + gap) + edge / 2.0f;
			float centerY = gap + v.y * (edge + gap) + edge / 2.0f;

			ctx.Transform =
				Matrix3x2.Translation(-edge / 2, -edge / 2) *
				Matrix3x2.Translation(centerX, centerY);

			ctx.FillRoundedRectangle(new RoundedRectangle
			{
				RadiusX = edge / 21,
				RadiusY = edge / 21,
				Rect = new RectangleF(0, 0, edge, edge),
			}, XResource.GetColor(new Color(0x59dae4ee)));
		}

		foreach (var p in MatrixPositions)
		{
			var c = Matrix.CellTable[p.y, p.x];
			if (c == null) continue;

			float centerX = gap + p.x * (edge + gap) + edge / 2.0f;
			float centerY = gap + p.y * (edge + gap) + edge / 2.0f;

			ctx.Transform =
				Matrix3x2.Translation(-edge / 2, -edge / 2) *
				Matrix3x2.Translation(centerX, centerY);
			ctx.FillRectangle(new RectangleF(0, 0, edge, edge), XResource.GetColor(c.DisplayInfo.Background));

			var textLayout = XResource.TextLayouts[c.N.ToString(), c.DisplayInfo.FontSize];
			ctx.Transform =
				Matrix3x2.Translation(-textLayout.Metrics.Width / 2, -textLayout.Metrics.Height / 2) *
				Matrix3x2.Translation(centerX, centerY);
			ctx.DrawTextLayout(Vector2.Zero, textLayout, XResource.GetColor(c.DisplayInfo.Foreground));
		}
	}
}

class Cell
{
	public int N;

	public Cell(int n)
	{
		N = n;
	}

	public DisplayInfo DisplayInfo => N switch
	{
		2 => DisplayInfo.Create(),
		4 => DisplayInfo.Create(0xede0c8ff),
		8 => DisplayInfo.Create(0xf2b179ff, 0xf9f6f2ff),
		16 => DisplayInfo.Create(0xf59563ff, 0xf9f6f2ff),
		32 => DisplayInfo.Create(0xf67c5fff, 0xf9f6f2ff),
		64 => DisplayInfo.Create(0xf65e3bff, 0xf9f6f2ff),
		128 => DisplayInfo.Create(0xedcf72ff, 0xf9f6f2ff, 45),
		256 => DisplayInfo.Create(0xedcc61ff, 0xf9f6f2ff, 45),
		512 => DisplayInfo.Create(0xedc850ff, 0xf9f6f2ff, 45),
		1024 => DisplayInfo.Create(0xedc53fff, 0xf9f6f2ff, 35),
		2048 => DisplayInfo.Create(0x3c3a32ff, 0xf9f6f2ff, 35),
		_ => DisplayInfo.Create(0x3c3a32ff, 0xf9f6f2ff, 30),
	};

	static Random r = new Random();
	public static Cell CreateRandom() => new Cell(r.NextDouble() < 0.9 ? 2 : 4);
}

class Matrix
{
	public Cell[,] CellTable;
	static (int x, int y)[] Directions = new[] { (0, -1), (0, 1), (-1, 0), (1, 0) };

	public IEnumerable<Cell> GetCells()
	{
		foreach (var c in CellTable)
			if (c != null) yield return c;
	}

	public int GetScore() => GetCells().Sum(v => v.N);

	public void ReInitialize()
	{
		CellTable = new Cell[MatrixSize, MatrixSize];

		CellTable[0, 0] = new Cell(2);
		CellTable[0, 1] = new Cell(4);
		CellTable[0, 2] = new Cell(8);
		CellTable[0, 3] = new Cell(16);
		CellTable[1, 0] = new Cell(32);
		CellTable[1, 1] = new Cell(64);
		CellTable[1, 2] = new Cell(128);
		CellTable[1, 3] = new Cell(256);
		CellTable[2, 0] = new Cell(512);
		CellTable[2, 1] = new Cell(1024);
		CellTable[2, 2] = new Cell(2048);
		CellTable[2, 3] = new Cell(4096);
		CellTable[3, 0] = new Cell(8192);
		CellTable[3, 1] = new Cell(16384);
		CellTable[3, 2] = new Cell(32768);
		CellTable[3, 3] = new Cell(65536);
	}
}

struct DisplayInfo
{
	public Color Background;
	public Color Foreground;
	public float FontSize;

	public static DisplayInfo Create(uint background = 0xeee4daff, uint color = 0x776e6fff, float fontSize = 55) =>
		new DisplayInfo { Background = new Color(background), Foreground = new Color(color), FontSize = fontSize };
}

static class RandomUtil
{
	static Random r = new Random();
	public static T[] ShuffleCopy<T>(this IEnumerable<T> data)
	{
		var arr = data.ToArray();

		for (var i = arr.Length - 1; i > 0; --i)
		{
			int randomIndex = r.Next(i + 1);

			T temp = arr[i];
			arr[i] = arr[randomIndex];
			arr[randomIndex] = temp;
		}

		return arr;
	}
}