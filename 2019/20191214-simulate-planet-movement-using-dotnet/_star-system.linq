<Query Kind="Program">
  <NuGetReference>FlysEngine.Desktop</NuGetReference>
  <Namespace>FlysEngine.Desktop</Namespace>
  <Namespace>SharpDX</Namespace>
  <Namespace>SharpDX.Direct2D1</Namespace>
  <Namespace>SharpDX.DXGI</Namespace>
  <Namespace>System.Windows.Forms</Namespace>
</Query>

void Main()
{
    StarSystem.Run(StarSystem.CreateNSystem(4));
}

class StarSystem
{
    public const double StepDt = 0.01666;
    public List<Star> Stars { get;}
    public StarSystem(List<Star> stars)
    {
        Stars = stars;
    }
    
    double ac = 0;
    public int AutoStep(float dt)
    {
        int times = 0;
        for (ac += dt; ac >= StarSystem.StepDt; ac -= StarSystem.StepDt, times++)
        {
            Step();
        }
        return times;
    }
    
    void Step()
    {
        foreach (var s1 in Stars)
        {
            // star velocity
            // F = G * m1 * m2 / r^2
            // F has a direction: 
            double Fdx = 0;
            double Fdy = 0;

            const double Gm1 = 100.0f;     // G*s1.m
            var ttm = StepDt * StepDt; // t*t/s1.m

            foreach (var s2 in Stars)
            {
                if (s1 == s2) continue;

                var rx = s2.Px - s1.Px;
                var ry = s2.Py - s1.Py;
                var rr = rx * rx + ry * ry;
                var r = Math.Sqrt(rr);

                var f = Gm1 * s2.Mass / rr;
                var fdx = f * rx / r;
                var fdy = f * ry / r;

                Fdx += fdx;
                Fdy += fdy;
            }

            // Ft = ma	-> a = Ft/m
            // v  = at	-> v = Ftt/m
            var dvx = Fdx * ttm;
            var dvy = Fdy * ttm;
            s1.Vx += dvx;
            s1.Vy += dvy;
        }

        foreach (var star in Stars)
        {
            star.Move(StepDt);
        }
    }
    
    public void Draw(DeviceContext ctx)
    {
        ctx.Clear(Color.DarkGray);
        
        using var solidBrash = new SolidColorBrush(ctx, Color.White);

        float allHeight = ctx.Size.Height;
        float allWidth = ctx.Size.Width;
        float scale = allHeight / 100.0f;
        foreach (var star in Stars)
		{
			using var gsc = new SharpDX.Direct2D1.GradientStopCollection(ctx, new[]
			{
				new GradientStop{ Color = Color.White, Position = 0f},
				new GradientStop{ Color = star.Color, Position = 1.0f},
			});
			using var radialBrush = new RadialGradientBrush(ctx, new RadialGradientBrushProperties
            {
                Center = Vector2.Zero,
                RadiusX = 1.0f,
                RadiusY = 1.0f,
            }, gsc);

            ctx.Transform =
                Matrix3x2.Scaling(star.Size) *
                Matrix3x2.Translation(((float)star.Px + 50) * scale + (allWidth - allHeight) / 2, ((float)star.Py + 50) * scale);
            ctx.FillEllipse(new Ellipse(Vector2.Zero, 1, 1), radialBrush);

            ctx.Transform =
                Matrix3x2.Translation(allHeight / 2 + (allWidth - allHeight) / 2, allHeight / 2);
            foreach (var line in star.PositionTrack.Zip(star.PositionTrack.Skip(1)))
            {
                ctx.DrawLine(line.First * scale, line.Second * scale, solidBrash, 1.0f);
            }
        }
        ctx.Transform = Matrix3x2.Identity;
    }

    public static StarSystem CreateNSystem(int N) => new StarSystem(CreateStars(N).ToList());

    static IEnumerable<Star> CreateStars(int N)
    {
        for (var i = 0; i < N; ++i)
        {
            double angle = 1.0f * i / N * Math.PI * 2;
            double R = 45;
            double M = 10000 * 2 / (N * Math.Sqrt(N) * Math.Log(N));
            double v = 5;
            double px = R * Math.Sin(angle);
            double py = R * -Math.Cos(angle);
            double vx = v * Math.Cos(angle);
            double vy = v * Math.Sin(angle);
            yield return new Star
            {
                Px = px,
                Py = py,
                Vx = vx,
                Vy = vy,
                Mass = M,
            };
        }
    }
    
    public static StarSystem CreateSolarEarthMoon()
    {
        var solar = new Star
        {
            Px = 0, Py = 0,
            Vx = 0.6, Vy = 0,
            Mass = 1000,
            Color = Color.Red,
        };

        // Earth
        var earth = new Star
        {
            Px = 0, Py = -41,
            Vx = -5, Vy = 0,
            Mass = 100,
            Color = Color.Blue,
        };

        // Moon
        var moon = new Star
        {
            Px = 0, Py = -45,
            Vx = -10, Vy = 0,
            Mass = 10,
        };

        return new StarSystem(new List<Star> { solar, earth, moon });
    }
    
    public static void Run(StarSystem ss)
    {
        using var window = new RenderWindow
        {
            StartPosition = FormStartPosition.CenterScreen,
            Size = new System.Drawing.Size(800, 600),
        };

        window.UpdateLogic += (o, dt) => ss.AutoStep(dt);
        window.Draw += (o, ctx) => ss.Draw(ctx);

        RenderLoop.Run(window, () => window.Render(1, PresentFlags.None));
    }
}

class Star
{
    public LinkedList<Vector2> PositionTrack = new LinkedList<SharpDX.Vector2>();
    public double Px, Py, Vx, Vy;
    public double Mass;
    public float Size => (float)Math.Log(Mass) * 2;
    public Color Color = Color.Black;
    
    public void Move(double step)
    {
        Px += Vx * step;
        Py += Vy * step;
        PositionTrack.AddFirst(new Vector2((float)Px, (float)Py));
        if (PositionTrack.Count > 1000)
        {
            PositionTrack.RemoveLast();
        }
    }
}