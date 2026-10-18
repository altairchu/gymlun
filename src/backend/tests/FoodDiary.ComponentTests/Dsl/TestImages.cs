using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace FoodDiary.ComponentTests.Dsl;

public static class TestImages
{
    public static byte[] Png(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(255, 165, 0));
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }

    public static byte[] Jpeg(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(255, 165, 0));
        using var stream = new MemoryStream();
        image.SaveAsJpeg(stream);
        return stream.ToArray();
    }

    public static Size SizeOf(byte[] image) => Image.Identify(image).Size;
}
