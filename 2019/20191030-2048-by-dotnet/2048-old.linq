<Query Kind="Program">
  <NuGetReference>FlysEngine.Desktop</NuGetReference>
  <NuGetReference>MiSe.Shuffle</NuGetReference>
  <NuGetReference>System.Reactive</NuGetReference>
  <Namespace>FlysEngine.Desktop</Namespace>
  <Namespace>MiSe.Shuffle.Extensions</Namespace>
  <Namespace>SharpDX.Direct2D1</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Windows.Forms</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Reactive</Namespace>
  <Namespace>SharpDX</Namespace>
  <Namespace>SharpDX.Animation</Namespace>
  <Namespace>SharpDX.DXGI</Namespace>
</Query>

#load ".\_mouse-gesture.linq"
static Random r = new Random();
const int MatrixSize = 4;

void Main()
{
    using var g = new GameWindow();
    RenderLoop.Run(g, () => g.Render(1, PresentFlags.None));
}

public class GameWindow : RenderWindow
{
    Matrix Matrix = new Matrix();
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

    protected override void OnLoad(EventArgs e)
    {
        Instance = this;
        ClientSize = new System.Drawing.Size(300, 300);
        Observable.FromEventPattern<KeyEventArgs>(this, nameof(this.KeyUp))
            .Select(x => x.EventArgs.KeyCode)
            .Select(x => x switch
            {
                Keys.Left => (Direction?)Direction.Left,
                Keys.Right => Direction.Right,
                Keys.Down => Direction.Down,
                Keys.Up => Direction.Up,
                _ => null
            })
            .Where(x => x != null)
            .Select(x => x.Value).Merge(DetectMouseGesture(this))
            .Subscribe(direction => Matrix.RequestDirection(direction));
            
        Matrix.ReInitialize();
    }

    protected override void OnUpdateLogic(float dt)
    {
        base.OnUpdateLogic(dt);

        if (Matrix.GameOver)
        {
            MessageBox.Show($"你的总分是：{Matrix.Cells.Sum(v => v.N)}", "Game Over");
            Matrix.ReInitialize();
        }
    }

    protected override void OnDraw(DeviceContext ctx)
    {
        ctx.Clear(Color.CornflowerBlue);

        ctx.Transform = Matrix3x2.Identity;
        float edgeWidth = ctx.Size.Width / MatrixSize;
        float edgeHeight = ctx.Size.Height / MatrixSize;
        for (var i = 0; i < MatrixSize; ++i)
        {
            for (var j = 0; j < MatrixSize; ++j)
            {
                ctx.DrawRectangle(new RectangleF(i * edgeWidth, j * edgeHeight, edgeWidth, edgeHeight), XResource.GetColor(Color.Black));
            }
        }

        foreach (var c in Matrix.Cells)
        {
            float offset = 2.0f;
            float cellWidth = edgeWidth - offset * 2;
            float cellHeight = edgeHeight - offset * 2;
            float centerX = c.DisplayX * edgeWidth + offset + cellWidth / 2.0f;
            float centerY = c.DisplayY * edgeHeight + offset + cellHeight / 2.0f;

            ctx.Transform =
                Matrix3x2.Translation(-cellWidth / 2, -cellHeight / 2) *
                Matrix3x2.Scaling(c.DisplaySize) *
                Matrix3x2.Translation(centerX, centerY);
            ctx.FillRectangle(new RectangleF(0, 0, cellWidth, cellHeight), XResource.GetColor(c.DisplayInfo.BackgroundColor));

            var textLayout = XResource.TextLayouts[c.N.ToString(), c.DisplayInfo.FontSize];
            ctx.Transform =
                Matrix3x2.Translation(-textLayout.Metrics.Width / 2, -textLayout.Metrics.Height / 2) *
                Matrix3x2.Scaling(c.DisplaySize) *
                Matrix3x2.Translation(centerX, centerY);
            ctx.DrawTextLayout(Vector2.Zero, textLayout, XResource.GetColor(c.DisplayInfo.ForegroundColor));
        }
    }
}

class Box
{
    public int X, Y, N;
    public Box(int x, int y, int n) { X = x; Y = y; N = n; }
}

class Cell : Box
{
    public float DisplayX, DisplayY, DisplaySize = 0;
    public bool Deleted = false;
    const float AnimationDurationMs = 100;

    public Cell(int x, int y, int n) : base(x, y, n)
    {
        GameWindow.CreateAnimation(DisplaySize, 1, AnimationDurationMs, v => DisplaySize = v);
        DisplayX = x; DisplayY = y;
    }

    public void MoveTo(int x, int y, int n = default)
    {
        X = x; Y = y; if (n != default) N = n;
        GameWindow.CreateAnimation(DisplayX, x, AnimationDurationMs, v => DisplayX = v);
        GameWindow.CreateAnimation(DisplayY, y, AnimationDurationMs, v => DisplayY = v);
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

    public Box CreateCopyBox() => new Box(X, Y, N);

    public static Cell CreateRandomAt(int x, int y) => new Cell(x, y, r.NextDouble() < 0.9 ? 2 : 4);
}

class Matrix
{
    public List<Cell> Cells { get; private set; } = new List<Cell>();
    Stack<List<Box>> CellHistory = new Stack<List<Box>>();
    public bool GameOver { get; set; }

    public void ReInitialize()
    {
        Cells.Clear(); CellHistory.Clear();
        (int x, int y)[] emptyPositions = Enumerable.Range(0, MatrixSize)
            .SelectMany(y => Enumerable.Range(0, MatrixSize).Select(x => (x, y)))
            .ShuffleCopy(r);
        Cells.Add(Cell.CreateRandomAt(emptyPositions[0].x, emptyPositions[0].y));
        Cells.Add(Cell.CreateRandomAt(emptyPositions[1].x, emptyPositions[1].y));
    }
    
    public bool MoveAvailable()
    {
        // Only Occurs in full condition
        if (Cells.Count != MatrixSize * MatrixSize) return false;
        
        Cell[][] cellTable = GetCellTable();
        throw new NotImplementedException();
    }
    
    public Cell[][] GetCellTable() => Enumerable.Range(0, MatrixSize)
            .Select(y => Enumerable
                .Range(0, MatrixSize).Select(x => (x, y))
                .Select(v => Cells.FirstOrDefault(x => x.X == v.x && x.Y == v.y))
                .ToArray())
            .ToArray();

    public void RequestDirection(Direction direction)
    {
        if (GameOver) return;

        List<Box> history = Cells.Select(x => x.CreateCopyBox()).ToList();
        IOrderedEnumerable<Cell> orderedCells = direction switch
        {
            Direction.Up => Cells.OrderBy(c => c.Y),
            Direction.Down => Cells.OrderByDescending(c => c.Y),
            Direction.Left => Cells.OrderBy(c => c.X),
            Direction.Right => Cells.OrderByDescending(c => c.X),
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };

        var combinedCells = new List<Cell>();
        bool moved = false;
        Cell[][] table = GetCellTable();
        
        foreach (Cell cell in orderedCells.Where(x => !x.Deleted))
        {
            var targetCell = cell.CreateCopyBox();

            foreach ((int x, int y) position in (direction switch
            {
                Direction.Up => Enumerable.Range(0, cell.Y).Select((v, i) => (cell.X, y: cell.Y - i - 1)),
                Direction.Down => Enumerable.Range(0, MatrixSize - cell.Y - 1).Select(v => (cell.X, cell.Y + v + 1)),
                Direction.Left => Enumerable.Range(0, cell.X).Select((v, i) => (cell.X - i - 1, cell.Y)),
                Direction.Right => Enumerable.Range(0, MatrixSize - cell.X - 1).Select(v => (cell.X + v + 1, cell.Y)),
                _ => throw new ArgumentOutOfRangeException(nameof(direction)),
            }))
            {
                Cell nextCell = table[position.y][position.x];
                if (nextCell == null)
                {
                    targetCell.X = position.x;
                    targetCell.Y = position.y;
                }
                else if (!combinedCells.Contains(nextCell) && nextCell.N == targetCell.N)
                {
                    targetCell.X = position.x;
                    targetCell.Y = position.y;
                    targetCell.N += nextCell.N;
                    nextCell.Deleted = true;
                    combinedCells.Add(cell);
                    break;
                }
                else
                {
                    break;
                }
            }

            if (targetCell.X != cell.X || targetCell.Y != cell.Y)
            {
                moved = true;
                cell.MoveTo(targetCell.X, targetCell.Y, targetCell.N);
            }
        }

        if (moved)
        {
            Cells.RemoveAll(x => x.Deleted);
            CellHistory.Push(Cells.Select(v => v.CreateCopyBox()).ToList());
            (int x, int y)[] emptyPositions = Enumerable.Range(0, MatrixSize)
                .SelectMany(y => Enumerable.Range(0, MatrixSize).Select(x => (x, y)))
                .Where(v => !Cells.Any(c => c.X == v.x && c.Y == v.y))
                .ShuffleCopy(r);
            if (emptyPositions.Length != 0)
            {
                Cells.Add(Cell.CreateRandomAt(emptyPositions[0].x, emptyPositions[0].y));
            }
            else
            {
                GameOver = true;
            }
        }
    }

    public void PopHistory()
    {
        if (CellHistory.TryPop(out List<Box> history))
        {
            Cells = history.Select(v => new Cell(v.X, v.Y, v.N)).ToList();
        }
    }
}

class DisplayInfo
{
    public Color BackgroundColor;
    public Color ForegroundColor;
    public float FontSize;

    public static DisplayInfo Create(uint background = 0xeee4daff, uint color = 0x776e6fff, float fontSize = 55) =>
        new DisplayInfo { BackgroundColor = new Color(background), ForegroundColor = new Color(color), FontSize = fontSize };
}