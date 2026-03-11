using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using NetworkDrive.Domain.Interfaces;
using NetworkDrive.Infrastructure.Storage;

namespace NetworkDrive.Infrastructure.Transcoding;

public class TranscodingService : ITranscodingService
{
    private readonly string _rootPath;
    private readonly TranscodingOptions _options;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    private static readonly HashSet<string> TranscodeExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".mkv", ".mov", ".avi", ".wmv", ".flv", ".ts", ".m2ts" };

    public TranscodingService(IOptions<StorageOptions> storageOptions, IOptions<TranscodingOptions> transcodingOptions)
    {
        _rootPath = storageOptions.Value.RootPath;
        _options = transcodingOptions.Value;

        if (string.IsNullOrWhiteSpace(_options.CacheDirectory))
        {
            _options.CacheDirectory = Path.Combine(Path.GetTempPath(), "NetworkDrive", "TranscodeCache");
        }

        Directory.CreateDirectory(_options.CacheDirectory);
    }

    public bool RequiresTranscoding(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return TranscodeExtensions.Contains(ext);
    }

    public async Task<Stream> GetTranscodedStreamAsync(string relativePath, CancellationToken ct = default)
    {
        var sourcePath = ResolvePath(relativePath);
        var cacheKey = GenerateCacheKey(relativePath);
        var cachedPath = Path.Combine(_options.CacheDirectory, cacheKey + ".mp4");

        var fileLock = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await fileLock.WaitAsync(ct);

        try
        {
            if (File.Exists(cachedPath))
            {
                var sourceModified = File.GetLastWriteTimeUtc(sourcePath);
                var cacheModified = File.GetLastWriteTimeUtc(cachedPath);

                if (cacheModified >= sourceModified)
                    return new FileStream(cachedPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            }

            await TranscodeAsync(sourcePath, cachedPath, ct);
            return new FileStream(cachedPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        }
        finally
        {
            fileLock.Release();
        }
    }

    private async Task TranscodeAsync(string inputPath, string outputPath, CancellationToken ct)
    {
        var tempOutput = outputPath + ".tmp";

        try
        {
            // First attempt: copy video stream, only re-encode audio to AAC (fast).
            var (exitCode, stdErr) = await RunFFmpegAsync(
                $"-i \"{inputPath}\" -c:v copy -c:a aac -b:a 192k -movflags +faststart -f mp4 -y \"{tempOutput}\"",
                ct);

            if (exitCode != 0)
            {
                if (File.Exists(tempOutput))
                    File.Delete(tempOutput);

                // Fallback: full transcode of both video and audio.
                (exitCode, stdErr) = await RunFFmpegAsync(
                    $"-i \"{inputPath}\" -c:v libx264 -preset fast -crf 23 -c:a aac -b:a 192k -movflags +faststart -y \"{tempOutput}\"",
                    ct);

                if (exitCode != 0)
                    throw new InvalidOperationException(
                        $"FFmpeg transcoding failed (exit code {exitCode}). FFmpeg output: {stdErr}");
            }

            File.Move(tempOutput, outputPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempOutput))
                File.Delete(tempOutput);
        }
    }

    private async Task<(int ExitCode, string StdErr)> RunFFmpegAsync(string arguments, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _options.FFmpegPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start FFmpeg. Ensure it is installed and the path is correct.");

        // Drain stdout/stderr to prevent pipe buffer deadlocks.
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);
        await Task.WhenAll(stderrTask, stdoutTask);

        return (process.ExitCode, await stderrTask);
    }

    private string ResolvePath(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));
        if (!fullPath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Path traversal detected.");
        return fullPath;
    }

    private static string GenerateCacheKey(string relativePath)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(relativePath.ToLowerInvariant()));
        return Convert.ToHexString(bytes)[..32];
    }
}
