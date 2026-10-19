using SkiaSharp;

namespace FoodDiary.API.Features.ProductPhotos;

public class ProductPhotoProcessor
{
    public const int MaxUploadBytes = 10 * 1024 * 1024;
    public const int MaxSide = 640;

    // A small file can still decode into a huge bitmap
    private const long MaxSourcePixels = 50_000_000;

    private const int JpegQuality = 80;

    private static readonly SKSamplingOptions Sampling = new(SKCubicResampler.Mitchell);

    /// <summary>
    /// Turns any common image format into an upright JPEG no larger than <see cref="MaxSide"/> on either side,
    /// without the metadata (such as where it was taken) the original carried
    /// </summary>
    /// <returns>Null when <paramref name="source"/> is not an image</returns>
    public async Task<byte[]?> ToJpeg(Stream source, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken);

        using var data = SKData.CreateCopy(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
        using var codec = SKCodec.Create(data);

        if (codec is null || (long)codec.Info.Width * codec.Info.Height > MaxSourcePixels)
        {
            return null;
        }

        var origin = codec.EncodedOrigin;
        var turnsSideways = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

        var stored = codec.Info.Size;
        var scale = Math.Min(1f, (float)MaxSide / Math.Max(stored.Width, stored.Height));
        var target = new SKSizeI(
            Math.Max(1, (int)Math.Round(stored.Width * scale)),
            Math.Max(1, (int)Math.Round(stored.Height * scale)));

        // JPEGs can be decoded straight at a fraction of their size, which keeps big phone photos small in memory
        var decodedSize = codec.GetScaledDimensions(scale);
        var decodedInfo = new SKImageInfo(
            decodedSize.Width,
            decodedSize.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul,
            SKColorSpace.CreateSrgb());

        using var decoded = SKBitmap.Decode(codec, decodedInfo);

        if (decoded is null)
        {
            return null;
        }

        var outputInfo = turnsSideways
            ? new SKImageInfo(target.Height, target.Width)
            : new SKImageInfo(target.Width, target.Height);

        using var surface = SKSurface.Create(outputInfo);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);
        canvas.Concat(ToUpright(origin, target.Width, target.Height));
        canvas.Scale((float)target.Width / decoded.Width, (float)target.Height / decoded.Height);

        using var image = SKImage.FromBitmap(decoded);
        canvas.DrawImage(image, 0, 0, Sampling);

        using var snapshot = surface.Snapshot();
        using var jpeg = snapshot.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
        return jpeg.ToArray();
    }

    /// <summary>
    /// Maps a <paramref name="width"/> x <paramref name="height"/> image, as stored, onto how its EXIF
    /// orientation says it is meant to be seen
    /// </summary>
    private static SKMatrix ToUpright(SKEncodedOrigin origin, float width, float height) => origin switch
    {
        SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, width, 0, 1, 0, 0, 0, 1),
        SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, width, 0, -1, height, 0, 0, 1),
        SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, height, 0, 0, 1),
        SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightTop => new SKMatrix(0, -1, height, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, height, -1, 0, width, 0, 0, 1),
        SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, width, 0, 0, 1),
        _ => SKMatrix.Identity
    };
}
