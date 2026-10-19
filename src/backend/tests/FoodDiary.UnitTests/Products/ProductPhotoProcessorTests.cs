using FoodDiary.API.Features.ProductPhotos;
using SkiaSharp;

namespace FoodDiary.UnitTests.Products;

public class ProductPhotoProcessorTests
{
    private readonly ProductPhotoProcessor _processor = new();

    [Fact]
    public async Task Large_photo_is_shrunk_to_fit_the_longest_side()
    {
        var photo = await ToJpeg(RedLeftBlueRight(1600, 1200, SKEncodedImageFormat.Png));

        Decode(photo).Info.Size.Should().Be(new SKSizeI(640, 480));
    }

    [Fact]
    public async Task Small_photo_keeps_its_size()
    {
        var photo = await ToJpeg(RedLeftBlueRight(120, 80, SKEncodedImageFormat.Png));

        Decode(photo).Info.Size.Should().Be(new SKSizeI(120, 80));
    }

    [Fact]
    public async Task Sideways_phone_photo_is_turned_upright_and_loses_its_orientation_tag()
    {
        var rotateClockwise = WithExifOrientation(RedLeftBlueRight(300, 100, SKEncodedImageFormat.Jpeg), 6);

        var photo = await ToJpeg(rotateClockwise);

        using var codec = SKCodec.Create(SKData.CreateCopy(photo));
        codec.EncodedOrigin.Should().Be(SKEncodedOrigin.TopLeft);
        var upright = Decode(photo);
        upright.Info.Size.Should().Be(new SKSizeI(100, 300));
        IsRed(upright.GetPixel(50, 20)).Should().BeTrue();
        IsBlue(upright.GetPixel(50, 280)).Should().BeTrue();
    }

    [Fact]
    public async Task Text_is_not_a_photo()
    {
        using var source = new MemoryStream("not a photo"u8.ToArray());

        var photo = await _processor.ToJpeg(source, CancellationToken.None);

        photo.Should().BeNull();
    }

    private async Task<byte[]> ToJpeg(byte[] source)
    {
        using var stream = new MemoryStream(source);
        var photo = await _processor.ToJpeg(stream, CancellationToken.None);
        photo.Should().NotBeNull();
        return photo!;
    }

    private static SKBitmap Decode(byte[] jpeg) => SKBitmap.Decode(jpeg);

    private static bool IsRed(SKColor color) => color.Red > 200 && color.Blue < 60;

    private static bool IsBlue(SKColor color) => color.Blue > 200 && color.Red < 60;

    private static byte[] RedLeftBlueRight(int width, int height, SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Blue);
            using var red = new SKPaint { Color = SKColors.Red };
            canvas.DrawRect(0, 0, width / 2f, height, red);
        }

        using var data = bitmap.Encode(format, 95);
        return data.ToArray();
    }

    private static byte[] WithExifOrientation(byte[] jpeg, ushort orientation)
    {
        byte[] exif =
        [
            0xFF, 0xE1, 0x00, 0x22,
            (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0x00, 0x00,
            (byte)'I', (byte)'I', 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00,
            0x01, 0x00,
            0x12, 0x01, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, (byte)orientation, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00
        ];

        return [..jpeg[..2], ..exif, ..jpeg[2..]];
    }
}
