<Query Kind="Statements">
  <NuGetReference>FlysEngine.Desktop</NuGetReference>
  <Namespace>FlysEngine.Desktop</Namespace>
  <Namespace>SharpDX</Namespace>
  <Namespace>SharpDX.Direct2D1</Namespace>
</Query>

Color Color_Background = Color.CornflowerBlue;
Color Color_Black = Color.Black;
Color Color_White = Color.White;
const float Speed = 0.25f;

using var taiji = new RenderWindow { Text = "太极" };
using var arc = new PathGeometry(taiji.XResource.Direct2DFactory);
var sink = arc.Open();
sink.BeginFigure(new Vector2(-1f, 0), FigureBegin.Filled);
sink.AddArc(new ArcSegment
{
	Point = new Vector2(1f, 0), 
	Size = new Size2F(1f, 1f), 
	RotationAngle = 0.0f, 
	SweepDirection = SweepDirection.Clockwise, 
	ArcSize = ArcSize.Large, 
});
sink.EndFigure(FigureEnd.Open);
sink.Close();

float angle = 0.0f;
taiji.UpdateLogic += (w, dt) =>
{
	angle += MathUtil.Mod2PI(Speed * MathUtil.TwoPi * dt);
};


taiji.Draw += (o, ctx) =>
{
	ctx.Clear(Color_Background);	
	
	float scale = GetR();
	Vector2 center = GetCenter();
	Matrix3x2 rotation = Matrix3x2.Rotation(angle);
	ctx.Transform = rotation * Matrix3x2.Scaling(scale, scale) * Matrix3x2.Translation(center);
	ctx.FillGeometry(arc, o.XResource.GetColor(Color_Black));

	ctx.Transform = rotation * Matrix3x2.Scaling(scale, scale) * Matrix3x2.Rotation((float)Math.PI) * Matrix3x2.Translation(center);
	ctx.FillGeometry(arc, o.XResource.GetColor(Color_White));
	
	ctx.Transform = rotation * Matrix3x2.Scaling(scale, scale) * Matrix3x2.Translation(center);
	ctx.FillEllipse(new Ellipse(new Vector2(0.5f, 0), 0.5f, 0.5f), o.XResource.GetColor(Color_Black));
	ctx.FillEllipse(new Ellipse(new Vector2(-0.5f, 0), 0.5f, 0.5f), o.XResource.GetColor(Color_White));

	ctx.FillEllipse(new Ellipse(new Vector2(0.5f, 0), 0.25f, 0.25f), o.XResource.GetColor(Color_White));
	ctx.FillEllipse(new Ellipse(new Vector2(-0.5f, 0), 0.25f, 0.25f), o.XResource.GetColor(Color_Black));
};

Vector2 GetCenter() => new Vector2(taiji.XResource.RenderTarget.Size.Width / 2, taiji.XResource.RenderTarget.Size.Height / 2);
float GetMinEdge() => Math.Min(taiji.XResource.RenderTarget.Size.Width, taiji.XResource.RenderTarget.Size.Height);
float GetR() => (GetMinEdge() - 5.0f) / 2;

RenderLoop.Run(taiji, () => taiji.Render(1, 0));