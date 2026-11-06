using System.Windows.Media.Imaging;
using System.IO;

namespace ApodHome.Core;

public sealed class ImageNormalizer
{
    public async Task SaveAsJpegAsync(Stream source, string destination, CancellationToken cancellationToken = default)
    {
        var temporary = destination + ".tmp";
        var download = destination + ".download-" + Path.GetRandomFileName();
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

        var moved = false;
        try
        {
            await CopyWithLimitAsync(source, download, cancellationToken).ConfigureAwait(false);
            await using var input = File.OpenRead(download);
            var decoder = BitmapDecoder.Create(input, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            if (decoder.Frames.Count == 0 || decoder.Frames[0].PixelWidth < 32 || decoder.Frames[0].PixelHeight < 32 ||
                (long)decoder.Frames[0].PixelWidth * decoder.Frames[0].PixelHeight > SafeHttp.MaxPixels)
                throw new InvalidDataException("Downloaded APOD image is too small or unreadable.");
            var encoder = new JpegBitmapEncoder { QualityLevel = 94 };
            encoder.Frames.Add(BitmapFrame.Create(decoder.Frames[0]));
            await using (var output = File.Create(temporary))
                encoder.Save(output);
            File.Move(temporary, destination, true);
            moved = true;
        }
        catch (NotSupportedException exception)
        {
            throw new InvalidDataException("APOD media is not a supported image format.", exception);
        }
        finally
        {
            if (File.Exists(download)) File.Delete(download);
            if (!moved && File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static async Task CopyWithLimitAsync(Stream source, string destination, CancellationToken cancellationToken)
    {
        await using var output = File.Create(destination);
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            total += read;
            if (total > SafeHttp.MaxImageBytes) throw new InvalidDataException("APOD image exceeds the 30 MB limit.");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }
    }
}
