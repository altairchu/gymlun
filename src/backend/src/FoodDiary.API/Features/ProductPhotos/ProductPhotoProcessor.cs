using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace FoodDiary.API.Features.ProductPhotos;

public class ProductPhotoProcessor
{
    public const int MaxUploadBytes = 10 * 1024 * 1024;
    public const int MaxSide = 640;

    // A small file can still decode into a huge bitmap
    private const long MaxSourcePixels = 50_000_000;

    private static readonly DecoderOptions DecoderOptions = new()
    {
        MaxFrames = 1,
        ColorProfileHandling = ColorProfileHandling.Convert
    };

    private static readonly JpegEncoder Encoder = new() { Quality = 80 };

    /// <summary>
    /// Turns any common image format into an upright JPEG no larger than <see cref="MaxSide"/> on either side,
    /// without the metadata (such as where it was taken) the original carried
    /// </summary>
    /// <returns>Null when <paramref name="source"/> is not an image</returns>
    public async Task<byte[]?> ToJpeg(Stream source, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken);

        try
        {
            buffer.Position = 0;
            var info = await Image.IdentifyAsync(DecoderOptions, buffer, cancellationToken);

            if ((long)info.Width * info.Height > MaxSourcePixels)
            {
                return null;
            }

            buffer.Position = 0;
            using var image = await Image.LoadAsync(DecoderOptions, buffer, cancellationToken);

            image.Mutate(context =>
            {
                context.AutoOrient();

                if (image.Width > MaxSide || image.Height > MaxSide)
                {
                    context.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(MaxSide, MaxSide)
                    });
                }
            });

            image.Metadata.ExifProfile = null;
            image.Metadata.XmpProfile = null;
            image.Metadata.IptcProfile = null;
            image.Metadata.IccProfile = null;

            using var output = new MemoryStream();
            await image.SaveAsJpegAsync(output, Encoder, cancellationToken);
            return output.ToArray();
        }
        catch (ImageFormatException)
        {
            return null;
        }
    }
}
