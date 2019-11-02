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

static IEnumerable<(int x, int y)> MatrixPositions => Enumerable
    .Range(0, MatrixSize)
    .SelectMany(y => Enumerable.Range(0, MatrixSize)
    .Select(x => (x, y)));

void Main()
{
    using var g = new GameWindow();
    RenderLoop.Run(g, () => g.Render(1, PresentFlags.None));
}

public class GameWindow : RenderWindow
{
    public GameWindow()
    {
        ClientSize = new System.Drawing.Size(400, 400);
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
    }
}