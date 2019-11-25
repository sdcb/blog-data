<Query Kind="Program">
  <NuGetReference Prerelease="true">Microsoft.Azure.CognitiveServices.Vision.Face</NuGetReference>
  <Namespace>System.Drawing.Imaging</Namespace>
  <Namespace>System.Drawing</Namespace>
</Query>

void Main()
{
    var bytes = CompressImage(@"D:\BaiduYunDownload\照片\IMG_0248 2.JPG");
    bytes.Length.Dump();
    Util.Image(bytes).Dump();
}

byte[] CompressImage(string image, int edgeLimit = 1920)
{
    using var bmp = Bitmap.FromFile(image);
    
    using var resized = (1.0 * Math.Max(bmp.Width, bmp.Height) / edgeLimit) switch
    {
        var x when x > 1 => new Bitmap(bmp, new Size((int)(bmp.Size.Width / x), (int)(bmp.Size.Height / x))), 
        _ => bmp, 
    };

    HandleOrientation(resized, bmp.PropertyItems);
    using var ms = new MemoryStream();
    resized.Save(ms, ImageFormat.Jpeg);
    return ms.ToArray();
}

void HandleOrientation(Image image, PropertyItem[] propertyItems)
{
    const int exifOrientationId = 0x112;
    PropertyItem orientationProp = propertyItems.FirstOrDefault(i => i.Id == exifOrientationId);
    
    if (orientationProp == null) return;
    
    int val = BitConverter.ToUInt16(orientationProp.Value, 0);
    RotateFlipType rotateFlipType = val switch
    {
        2 => RotateFlipType.RotateNoneFlipX, 
        3 => RotateFlipType.Rotate180FlipNone, 
        4 => RotateFlipType.Rotate180FlipX, 
        5 => RotateFlipType.Rotate90FlipX, 
        6 => RotateFlipType.Rotate90FlipNone, 
        7 => RotateFlipType.Rotate270FlipX, 
        8 => RotateFlipType.Rotate270FlipNone, 
        _ => RotateFlipType.RotateNoneFlipNone, 
    };
    
    if (rotateFlipType != RotateFlipType.RotateNoneFlipNone)
    {
        image.RotateFlip(rotateFlipType);
    }
}