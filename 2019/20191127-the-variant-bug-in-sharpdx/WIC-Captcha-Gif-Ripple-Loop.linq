<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Net.Http.dll</Reference>
  <NuGetReference>SharpDX</NuGetReference>
  <NuGetReference>SharpDX.Direct2D1</NuGetReference>
  <NuGetReference>SharpDX.Mathematics</NuGetReference>
  <Namespace>D2D = SharpDX.Direct2D1</Namespace>
  <Namespace>DWrite = SharpDX.DirectWrite</Namespace>
  <Namespace>Microsoft.Win32</Namespace>
  <Namespace>SharpDX</Namespace>
  <Namespace>SharpDX.IO</Namespace>
  <Namespace>SharpDX.Mathematics.Interop</Namespace>
  <Namespace>SharpDX.Win32</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Runtime.InteropServices</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>WIC = SharpDX.WIC</Namespace>
</Query>

void Main()
{
    byte[] gif = SaveD2DBitmap(200, 100, "HELLO");
    //File.WriteAllBytes(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\test.gif", gif);
    Util.Image(gif).Dump();
}

unsafe byte[] SaveD2DBitmap(int width, int height, string text)
{
    using var wic = new WIC.ImagingFactory2();
    using var d2d = new D2D.Factory();
    using var wicBitmap = new WIC.Bitmap(wic, width, height, WIC.PixelFormat.Format32bppPBGRA, WIC.BitmapCreateCacheOption.CacheOnDemand);
    using var target = new D2D.WicRenderTarget(d2d, wicBitmap, new D2D.RenderTargetProperties());
    using var dwriteFactory = new SharpDX.DirectWrite.Factory();
    using var brush = new D2D.SolidColorBrush(target, Color.Yellow);
    using var encoder = new WIC.GifBitmapEncoder(wic);

    using var ms = new MemoryStream();
    using var dc = target.QueryInterface<D2D.DeviceContext>();
    using var bmpLayer = new D2D.Bitmap1(dc, target.PixelSize,
        new D2D.BitmapProperties1(new D2D.PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, D2D.AlphaMode.Premultiplied),
        d2d.DesktopDpi.Width, d2d.DesktopDpi.Height,
        D2D.BitmapOptions.Target));

    var r = new Random();
    encoder.Initialize(ms);

    D2D.Image oldTarget = dc.Target;
    {
        dc.Target = bmpLayer;
        dc.BeginDraw();
        var textFormat = new DWrite.TextFormat(dwriteFactory, "Times New Roman",
            DWrite.FontWeight.Bold,
            DWrite.FontStyle.Normal,
            width / text.Length);
        for (int charIndex = 0; charIndex < text.Length; ++charIndex)
        {
            using var layout = new DWrite.TextLayout(dwriteFactory, text[charIndex].ToString(), textFormat, float.MaxValue, float.MaxValue);
            var layoutSize = new Vector2(layout.Metrics.Width, layout.Metrics.Height);
            using var b2 = new D2D.LinearGradientBrush(dc, new D2D.LinearGradientBrushProperties
            {
                StartPoint = Vector2.Zero,
                EndPoint = layoutSize,
            }, new D2D.GradientStopCollection(dc, new[]
            {
                new D2D.GradientStop{ Position = 0.0f, Color = ColorFromHsl(r.NextFloat(0, 1), 1.0f, 0.8f) },
                new D2D.GradientStop{ Position = 1.0f, Color = ColorFromHsl(r.NextFloat(0, 1), 1.0f, 0.8f) },
            }));

            var position = new Vector2(charIndex * width / text.Length, r.NextFloat(0, height - layout.Metrics.Height));
            dc.Transform =
                Matrix3x2.Translation(-layoutSize / 2) *
                Matrix3x2.Skew(r.NextFloat(0, 0.5f), r.NextFloat(0, 0.5f)) *
                //Matrix3x2.Rotation(r.NextFloat(0, MathF.PI * 2)) *
                Matrix3x2.Translation(position + layoutSize / 2);
            dc.DrawTextLayout(Vector2.Zero, layout, b2);
        }
        for (var i = 0; i < 4; ++i)
        {
            target.Transform = Matrix3x2.Identity;
            brush.Color = ColorFromHsl(r.NextFloat(0, 1), 1.0f, 0.3f);
            target.DrawLine(
                r.NextVector2(Vector2.Zero, new Vector2(width, height)),
                r.NextVector2(Vector2.Zero, new Vector2(width, height)),
                brush, 3.0f);
        }
        target.EndDraw();
    }

    Color background = ColorFromHsl(r.NextFloat(0, 1), 1.0f, 0.3f);
    var setMetadataMethod = encoder.MetadataQueryWriter
        .GetType()
        .GetMethod(nameof(WIC.MetadataQueryWriter.SetMetadataByName), BindingFlags.NonPublic | BindingFlags.Instance);
    {
        var setMetadata = (Action<string, IntPtr>)setMetadataMethod
            .CreateDelegate(typeof(Action<string, IntPtr>), encoder.MetadataQueryWriter);

        // /appext/Application: NETSCAPE2.0
        byte* bytes = stackalloc byte[11] { 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48 };
        PV pv = PV.CreateUByteVector(11, (IntPtr)bytes);
        setMetadata("/appext/Application", (IntPtr)(void*)&pv);

        // /appext/Data: 3, 1, [0, 0], 0
        byte* bytes2 = stackalloc byte[5] { 3, 1, 0, 0, 0, };
        PV pv2 = PV.CreateUByteVector(5, (IntPtr)bytes2);
        setMetadata("/appext/Data", (IntPtr)(void*)&pv2);

        // "/commentext/TextEntry": "Created by Flysha.Zhou\0"
        var commentsBytes = Encoding.UTF8.GetBytes("Created by Flysha.Zhou\0");
        fixed (byte* p = commentsBytes)
        {
            var pv3 = PV.CreateString((IntPtr)p);
            setMetadata("/commentext/TextEntry", (IntPtr)(void*)&pv3);
        }
    }
    for (var frameId = -10; frameId < 10; ++frameId)
    {
        dc.Target = null;
        using var displacement = new D2D.Effects.DisplacementMap(dc);
        displacement.SetInput(0, bmpLayer, true);
        displacement.Scale = Math.Abs(frameId) * 10.0f;

        var turbulence = new D2D.Effects.Turbulence(dc);
        displacement.SetInputEffect(1, turbulence);

        dc.Target = oldTarget;
        dc.BeginDraw();
        dc.Clear(background);
        dc.DrawImage(displacement);
        dc.EndDraw();

        using (var frame = new WIC.BitmapFrameEncode(encoder))
        {
            frame.Initialize();
            frame.SetSize(wicBitmap.Size.Width, wicBitmap.Size.Height);

            var setMetadata = (Action<string, IntPtr>)setMetadataMethod
                .CreateDelegate(typeof(Action<string, IntPtr>), frame.MetadataQueryWriter);
            var pv = PV.CreateUI2(5);
            setMetadata("/grctlext/Delay", (IntPtr)(void*)&pv);

            var pixelFormat = wicBitmap.PixelFormat;
            frame.SetPixelFormat(ref pixelFormat);
            frame.WriteSource(wicBitmap);

            frame.Commit();
        }
    }

    encoder.Commit();
    return ms.ToArray();
}

Color ColorFromHsl(float h, float s, float l)
{
    if (h > 1.0f)
    {
        h = h / 360.0f;
    }
    double r = 0, g = 0, b = 0;
    if (l == 0)
    {
        r = g = b = 0;
    }
    else
    {
        if (s == 0)
        {
            r = g = b = l;
        }
        else
        {
            var temp2 = ((l <= 0.5) ? l * (1.0 + s) : l + s - (l * s));
            var temp1 = (2.0 * l) - temp2;
            var t3 = new double[] { h + (1.0 / 3.0), h, h - (1.0 / 3.0) };
            var clr = new double[] { 0, 0, 0 };
            for (var i = 0; i < 3; i++)
            {
                if (t3[i] < 0)
                {
                    t3[i] += 1.0;
                }
                if (t3[i] > 1)
                {
                    t3[i] -= 1.0;
                }
                clr[i] = 6.0 * t3[i] < 1.0
                    ? temp1 + ((temp2 - temp1) * t3[i] * 6.0)
                    : 2.0 * t3[i] < 1.0 ? temp2 : 3.0 * t3[i] < 2.0
                    ? temp1 + ((temp2 - temp1) * ((2.0 / 3.0) - t3[i]) * 6.0) : temp1;
            }
            r = clr[0];
            g = clr[1];
            b = clr[2];
        }
    }
    return new Color((byte)(255 * r), (byte)(255 * g), (byte)(255 * b));
}

[StructLayout(LayoutKind.Explicit)]
struct PV
{
    [FieldOffset(0)] short VT;

    [FieldOffset(8)] int Length;

    [FieldOffset(8)] IntPtr StringBuffer;
    
    [FieldOffset(8)] ushort UShortValue;

    [FieldOffset(16)] IntPtr Buffer;

    public static PV CreateUByteVector(int length, IntPtr buffer) => new PV
    {
        VT = (short)VariantType.Vector + (short)VariantElementType.UByte,
        Length = length,
        Buffer = buffer
    };

    public static PV CreateString(IntPtr buffer) => new PV
    {
        VT = (short)VariantElementType.StringPointer,
        StringBuffer = buffer,
    };

    public static PV CreateUI2(ushort val) => new PV
    {
        VT = (short)VariantElementType.UShort,
        UShortValue = val,
    };
}