namespace NetworkDrive.Infrastructure.Transcoding;

public class TranscodingOptions
{
    public string FFmpegPath { get; set; } = "ffmpeg";
    public string CacheDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "NetworkDrive", "TranscodeCache");
}
