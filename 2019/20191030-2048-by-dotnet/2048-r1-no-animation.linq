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

#load ".\_mouse-gesture.linq"
const int MatrixSize = 4;

static IEnumerable<int> inorder = Enumerable.Range(0, MatrixSize);
static IEnumerable<(int x, int y)> MatrixPositions =>
	inorder.SelectMany(y => inorder.Select(x => (x, y)));

static bool WithinBounds((int x, int y) i) => i.x >= 0 && i.y >= 0 && i.x < MatrixSize && i.y < MatrixSize;

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

		var keyUp = Observable.FromEventPattern<KeyEventArgs>(this, nameof(this.KeyUp))
			.Select(x => x.EventArgs.KeyCode);

		keyUp.Select(x => x switch
			{
				Keys.Left => (Direction?)Direction.Left,
				Keys.Right => Direction.Right,
				Keys.Down => Direction.Down,
				Keys.Up => Direction.Up,
				_ => null
			})
			.Where(x => x != null)
			.Select(x => x.Value)
			.Merge(DetectMouseGesture(this))
			.Subscribe(direction =>
			{
				Matrix.RequestDirection(direction);
				Text = $"总分：{Matrix.GetScore()}";
			});

		keyUp.Where(k => k == Keys.Escape).Subscribe(k =>
		{
			if (MessageBox.Show("要重新开始游戏吗？", "确认", MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.OK)
			{
				Matrix.ReInitialize();
			}
		});
		keyUp.Where(k => k == Keys.Back).Subscribe(k => Matrix.TryPopHistory());
	}

	protected override void OnLoad(EventArgs e) => Matrix.ReInitialize();

	protected override void OnUpdateLogic(float dt)
	{
		base.OnUpdateLogic(dt);

		if (Matrix.GameOver)
		{
			if (MessageBox.Show($"总分：{Matrix.GetScore()}\r\n重新开始吗？", "失败！", MessageBoxButtons.YesNo) == DialogResult.Yes)
			{
				Matrix.ReInitialize();
			}
			else
			{
				Matrix.GameOver = false;
			}
		}
		else if (!Matrix.KeepGoing && Matrix.GetCells().Any(v => v.N == 2048))
		{
			if (MessageBox.Show("您获得了2048！\r\n还想继续升级吗？", "恭喜！", MessageBoxButtons.YesNo) == DialogResult.Yes)
			{
				Matrix.KeepGoing = true;
			}
			else
			{
				Matrix.ReInitialize();
			}
		}
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
	const float AnimationDurationMs = 20;

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

	public static Cell CreateRandom() => new Cell(RandomUtil.r.NextDouble() < 0.9 ? 2 : 4);
}

class Matrix
{
	public Cell[,] CellTable;
	Stack<int[]> CellHistory = new Stack<int[]>();
	public bool GameOver, KeepGoing;
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
		GameOver = false; KeepGoing = false; CellHistory.Clear();

		(int x, int y)[] allPos = MatrixPositions.ShuffleCopy();
		for (var i = 0; i < 2; ++i) // 2: initial cell count
		{
			CellTable[allPos[i].y, allPos[i].x] = Cell.CreateRandom();
		}
	}

	public void RequestDirection(Direction direction)
	{
		if (GameOver) return;

		var dv = Directions[(int)direction];
		var tx = dv.x == 1 ? inorder.Reverse() : inorder;
		var ty = dv.y == 1 ? inorder.Reverse() : inorder;

		bool moved = false;
		int[] history = CellTable.Cast<Cell>().Select(v => v?.N ?? default).ToArray();
		foreach (var i in tx.SelectMany(x => ty.Select(y => (x, y))))
		{
			Cell cell = CellTable[i.y, i.x];
			if (cell == null) continue;

			var next = NextCellInDirection(i, dv);

			if (WithinBounds(next.target) && CellTable[next.target.y, next.target.x].N == cell.N)
			{   // 对面有方块，且可合并
				CellTable[i.y, i.x] = null;
				CellTable[next.target.y, next.target.x] = cell;
				cell.N *= 2;
				moved = true;
			}
			else if (next.prev != i) // 对面无方块，移动到prev
			{
				CellTable[i.y, i.x] = null;
				CellTable[next.prev.y, next.prev.x] = cell;
				moved = true;
			}
		}

		if (moved)
		{
			CellHistory.Push(history);
			
			var nextPos = MatrixPositions
				.Where(v => CellTable[v.y, v.x] == null)
				.ShuffleCopy()
				.First();
			CellTable[nextPos.y, nextPos.x] = Cell.CreateRandom();

			if (!IsMoveAvailable()) GameOver = true;
		}
	}

	public ((int x, int y) target, (int x, int y) prev) NextCellInDirection((int x, int y) cell, (int x, int y) dv)
	{
		(int x, int y) prevCell;
		do
		{
			prevCell = cell;
			cell = (cell.x + dv.x, cell.y + dv.y);
		}
		while (WithinBounds(cell) && CellTable[cell.y, cell.x] == null);

		return (cell, prevCell);
	}

	public void TryPopHistory()
	{
		if (CellHistory.TryPop(out int[] history))
		{
			foreach (var pos in MatrixPositions)
			{
				CellTable[pos.y, pos.x] = history[pos.y * MatrixSize + pos.x] switch
				{
					default(int) => null,
					_ => new Cell(history[pos.y * MatrixSize + pos.x]),
				};
			}
		}
	}

	public bool IsMoveAvailable() => GetCells().Count() switch
	{
		MatrixSize * MatrixSize => MatrixPositions
			.SelectMany(v => Directions.Select(d => new
			{
				Position = v,
				Next = (x: v.x + d.x, y: v.y + d.y)
			}))
			.Where(x => WithinBounds(x.Position) && WithinBounds(x.Next))
			.Any(v => CellTable[v.Position.y, v.Position.x]?.N == CellTable[v.Next.y, v.Next.x]?.N),
		_ => true,
	};
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
	public static Random r = new Random();
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