<Query Kind="Statements">
  <NuGetReference>FlysEngine.Desktop</NuGetReference>
  <Namespace>FlysEngine.Desktop</Namespace>
  <Namespace>SharpDX</Namespace>
  <Namespace>SharpDX.Direct2D1</Namespace>
  <Namespace>SharpDX.DXGI</Namespace>
  <Namespace>SharpDX.Mathematics.Interop</Namespace>
  <Namespace>SharpDX.Direct2D1.Effects</Namespace>
  <Namespace>SharpDX.Win32</Namespace>
  <Namespace>SharpDX.Animation</Namespace>
</Query>

using var form = new RenderWindow { ClientSize = new System.Drawing.Size(400, 400) };
using var clockLineStyle = new StrokeStyle(form.XResource.Direct2DFactory, new StrokeStyleProperties 
{ 
	StartCap = CapStyle.Round, 
	EndCap = CapStyle.Triangle, 
});
float dpi = form.XResource.Direct2DFactory.DesktopDpi.Width;
Bitmap1 bitmap = null;
Shadow shadowEffect = null;
float secondPosition = 0;
Variable secondVariable = null;

var timer = new System.Windows.Forms.Timer { Enabled = true, Interval = 1000 };
timer.Tick += (o, e) =>
{
	secondVariable?.Dispose();
	secondVariable = form.XResource.CreateAnimation(secondPosition switch
	{
		59 => -1, 
		var x => x, 
	}, DateTime.Now.Second, 0.2f);
};

form.FormClosing += delegate { timer.Dispose(); };

form.UpdateLogic += (window, dt) =>
{
	secondPosition = (float)(secondVariable?.Value ?? DateTime.Now.Second);
};

form.CreateDeviceSizeResources += (RenderWindow sender) =>
{
	bitmap = new Bitmap1(form.XResource.RenderTarget, form.XResource.RenderTarget.PixelSize,
		new BitmapProperties1(new PixelFormat(Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied),
		dpi, dpi, BitmapOptions.Target));
	shadowEffect = new SharpDX.Direct2D1.Effects.Shadow(form.XResource.RenderTarget);
};

form.ReleaseDeviceSizeResources += o =>
{
	bitmap.Dispose();
	shadowEffect.Dispose();
	timer.Dispose();
};
	
form.Draw += (RenderWindow sender, DeviceContext ctx) =>
{
	ctx.Clear(Color.CornflowerBlue);
	
    float r = Math.Min(ctx.Size.Width, ctx.Size.Height) / 2 - 5;
    ctx.Transform = Matrix3x2.Translation(ctx.Size.Width/2, ctx.Size.Height/2);
    ctx.DrawEllipse(new Ellipse(Vector2.Zero, r, r), sender.XResource.GetColor(Color.Black), r / 60);
    
    for (var i = 0; i < 60; ++i)
    {
        ctx.Transform =
            Matrix3x2.Rotation(MathF.PI * 2 / 60 * i) *
            Matrix3x2.Translation(ctx.Size.Width / 2, ctx.Size.Height / 2);
        if (i % 5 == 0)
        {   // 时钟
            ctx.DrawLine(new Vector2(r - r / 15, 0), new Vector2(r, 0), form.XResource.GetColor(Color.Black), r/100);
        }
        else
        {   // 分钟
            ctx.DrawLine(new Vector2(r - r / 30, 0), new Vector2(r, 0), form.XResource.GetColor(Color.Black), r/200);
        }
    }
    
	ctx.EndDraw();
	
	var oldTarget = ctx.Target;
	ctx.Target = bitmap;
	ctx.BeginDraw();
    {
		var now = DateTime.Now;
		ctx.Clear(Color.Transparent);
        // 秒钟
		var blue = new Color(red: 0.0f, green: 0.0f, blue: 1.0f, alpha: 0.7f);
        ctx.Transform = 
            Matrix3x2.Rotation(MathF.PI * 2 / 60 * secondPosition) * 
            Matrix3x2.Translation(ctx.Size.Width / 2, ctx.Size.Height / 2);
        ctx.DrawLine(Vector2.Zero, new Vector2(0,-r*0.9f), form.XResource.GetColor(blue), r/50, clockLineStyle );
        
        // 分钟
		var green = new Color(red: 0.0f, green: 1.0f, blue: 0.0f, alpha: 0.7f);
        ctx.Transform =
            Matrix3x2.Rotation(MathF.PI * 2 / 60 * (now.Minute + now.Second / 60.0f)) *
            Matrix3x2.Translation(ctx.Size.Width / 2, ctx.Size.Height / 2);
        ctx.DrawLine(Vector2.Zero, new Vector2(0, -r * 0.8f), form.XResource.GetColor(green), r / 35, clockLineStyle);
        
        // 时钟
		var red = new Color(red: 1.0f, green: 0.0f, blue: 0.0f, alpha: 0.7f);
        ctx.Transform =
            Matrix3x2.Rotation(MathF.PI * 2 / 12 * (now.Hour + now.Minute / 60.0f)) *
            Matrix3x2.Translation(ctx.Size.Width / 2, ctx.Size.Height / 2);
        ctx.DrawLine(Vector2.Zero, new Vector2(0, -r * 0.7f), form.XResource.GetColor(red), r / 20, clockLineStyle);
    }
	ctx.EndDraw();
	shadowEffect.SetInput(0, ctx.Target, invalidate: new RawBool(false));

	ctx.Target = oldTarget;
	ctx.BeginDraw();
	{
		ctx.Transform = Matrix3x2.Identity;
		ctx.UnitMode = UnitMode.Pixels;
		ctx.DrawImage(shadowEffect, new Vector2(r/20,r/20));
		ctx.DrawBitmap(bitmap, 1.0f, InterpolationMode.NearestNeighbor);
		ctx.UnitMode = UnitMode.Dips;
	}
};
RenderLoop.Run(form, () => form.Render(1, PresentFlags.None));