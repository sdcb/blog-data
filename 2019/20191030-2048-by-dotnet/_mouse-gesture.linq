<Query Kind="Program">
  <NuGetReference Version="4.1.6">System.Reactive</NuGetReference>
  <Namespace>System.Windows.Forms</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
</Query>

void Main()
{
    using var form = new Form();
    DetectMouseGesture(form).Dump();

    Application.Run(form);
}

static IObservable<Direction> DetectMouseGesture(Form form)
{
    var mouseDown = Observable.FromEventPattern<MouseEventArgs>(form, nameof(form.MouseDown));
    var mouseUp = Observable.FromEventPattern<MouseEventArgs>(form, nameof(form.MouseUp));
    var mouseMove = Observable.FromEventPattern<MouseEventArgs>(form, nameof(form.MouseMove));
    const int throhold = 6;
    
    return mouseDown
        .SelectMany(x => mouseMove
        .TakeUntil(mouseUp)
        .Select(x => new { X = x.EventArgs.X, Y = x.EventArgs.Y })
        .ToList())
        .Select(d =>
        {
            int x = 0, y = 0;
            for (var i = 0; i < d.Count - 1; ++i)
            {
                if (d[i].X < d[i + 1].X) ++x;
                if (d[i].Y < d[i + 1].Y) ++y;
                if (d[i].X > d[i + 1].X) --x;
                if (d[i].Y > d[i + 1].Y) --y;
            }
            return (x, y);
		})
		.Select(v => new { Max = Math.Max(Math.Abs(v.x), Math.Abs(v.y)), Value = v})
        .Where(x => x.Max > throhold)
        .Select(v =>
        {
			if (v.Value.x == v.Max) return Direction.Right;
			if (v.Value.x == -v.Max) return Direction.Left;
			if (v.Value.y == v.Max) return Direction.Down;
			if (v.Value.y == -v.Max) return Direction.Up;
			throw new ArgumentOutOfRangeException(nameof(v));
        });
}

enum Direction
{
    Up, Down, Left, Right, 
}