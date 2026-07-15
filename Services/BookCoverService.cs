using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace LibWiseApp.Services;

public class BookCoverService
{
    private const int TargetWidth = 2560;
    private const int TargetHeight = 1600;
    public const long MaxUploadBytes = 15 * 1024 * 1024;

    public async Task<(byte[] Bytes, string ContentType)> ProcessAsync(Stream input, CancellationToken ct = default)
    {
        using var image = await Image.LoadAsync(input, ct);

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(TargetWidth, TargetHeight),
            Mode = ResizeMode.Crop,
            Position = AnchorPositionMode.Center
        }));

        using var ms = new MemoryStream();
        await image.SaveAsync(ms, new JpegEncoder { Quality = 85 }, ct);
        return (ms.ToArray(), "image/jpeg");
    }
}
