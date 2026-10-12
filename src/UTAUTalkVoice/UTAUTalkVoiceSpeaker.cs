using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using YukkuriMovieMaker.Plugin.Voice;

namespace UTAUTalkVoice;

public sealed class UTAUTalkVoiceSpeaker : IVoiceSpeaker
{
    static readonly SemaphoreSlim Semaphore = new(1);

    public string EngineName => "UTAU Talk";
    public string SpeakerName => "UTAU単独音";
    public string API => "UTAUTalk.CV";
    public string ID => "default";
    public bool IsVoiceDataCachingRequired => false;
    public SupportedTextFormat Format => SupportedTextFormat.Text;
    public IVoiceLicense? License => null;
    public IVoiceResource? Resource => null;

    public bool IsMatch(string api, string id) => api == API && id == ID;
    public IVoiceParameter CreateVoiceParameter() => new UTAUTalkVoiceParameter();
    public IVoiceParameter MigrateParameter(IVoiceParameter currentParameter) =>
        currentParameter is UTAUTalkVoiceParameter ? currentParameter : CreateVoiceParameter();
    public Task<string> ConvertKanjiToYomiAsync(string text, IVoiceParameter voiceParameter) =>
        throw new NotImplementedException();

    public async Task<IVoicePronounce?> CreateVoiceAsync(
        string text,
        IVoicePronounce? pronounce,
        IVoiceParameter? parameter,
        string filePath)
    {
        var p = parameter as UTAUTalkVoiceParameter ?? new UTAUTalkVoiceParameter();
        var voicebankDirectory = NormalizePath(p.VoicebankDirectory);
        if (!Directory.Exists(voicebankDirectory))
            throw new DirectoryNotFoundException($"UTAU音源フォルダが見つかりません: {voicebankDirectory}");

        var outputFilePath = Path.GetFullPath(filePath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath)!);
        await Semaphore.WaitAsync();
        try
        {
            var enginePath = EnsureUtauTts();
            var startInfo = new ProcessStartInfo
            {
                FileName = enginePath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            var toolsDirectory = Path.GetDirectoryName(enginePath)!;
            startInfo.WorkingDirectory = Directory.GetParent(toolsDirectory)?.FullName ?? toolsDirectory;
            var moraMilliseconds = 130.0 / Math.Max(0.25, p.SpeechRate);
            var intonationStrength = Math.Clamp(4.0 * p.Intonation * p.AccentStrength, 0.0, 8.0);
            string[] arguments = [
                "--voicebank", voicebankDirectory,
                "--text", text,
                "--prosody", "frame-intonation-tcn-v12",
                "--renderer", "utautts-world-phrase",
                "--mora-ms", moraMilliseconds.ToString(CultureInfo.InvariantCulture),
                "--intonation-strength", intonationStrength.ToString(CultureInfo.InvariantCulture),
                "--tone", MidiToneName(p.BaseNote),
                "--out", outputFilePath,
            ];
            foreach (var arg in arguments)
                startInfo.ArgumentList.Add(arg);

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("UTAU Talk合成エンジンを起動できません");
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            if (process.ExitCode != 0 || !File.Exists(outputFilePath))
                throw new InvalidOperationException(
                    $"UTAU Talkの音声生成に失敗しました（終了コード {process.ExitCode}）\n{stderr}\n{stdout}".Trim());
        }
        finally
        {
            Semaphore.Release();
        }

        return null;
    }

    static string NormalizePath(string? path) => (path ?? string.Empty).Trim().Trim('"').Trim();

    static string MidiToneName(int note)
    {
        string[] names = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
        note = Math.Clamp(note, 0, 127);
        return names[note % 12] + (note / 12 - 1).ToString(CultureInfo.InvariantCulture);
    }

    static string EnsureUtauTts()
    {
        var resourceDirectory = Path.Combine(
            AppContext.BaseDirectory, "user", "resources", "UTAUTalk", "utautts-1.5.1-japanese");
        var executablePath = Path.Combine(resourceDirectory, "tools", "utautts-cli.exe");
        if (File.Exists(executablePath))
            return executablePath;

        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? throw new InvalidOperationException("プラグインのフォルダを取得できません");
        var archivePath = Path.Combine(assemblyDirectory, "utautts.zip");
        if (!File.Exists(archivePath))
            throw new FileNotFoundException("同梱UtauTTSのアーカイブが見つかりません", archivePath);

        var parentDirectory = Path.GetDirectoryName(resourceDirectory)!;
        Directory.CreateDirectory(parentDirectory);
        var temporaryDirectory = resourceDirectory + ".tmp";
        if (Directory.Exists(temporaryDirectory))
            Directory.Delete(temporaryDirectory, true);
        ZipFile.ExtractToDirectory(archivePath, temporaryDirectory);
        if (!File.Exists(Path.Combine(temporaryDirectory, "tools", "utautts-cli.exe")))
            throw new InvalidDataException("utautts.zipにtools/utautts-cli.exeが含まれていません");
        if (Directory.Exists(resourceDirectory))
            Directory.Delete(resourceDirectory, true);
        MoveDirectoryWithRetry(temporaryDirectory, resourceDirectory);
        return executablePath;
    }

    static void MoveDirectoryWithRetry(string source, string destination)
    {
        Exception? lastError = null;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                Directory.Move(source, destination);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                lastError = ex;
                Thread.Sleep(200 * (attempt + 1));
            }
        }
        throw new IOException("UtauTTSの初回展開を完了できませんでした", lastError);
    }
}
