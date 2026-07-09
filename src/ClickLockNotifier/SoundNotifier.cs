using System.Reflection;
using System.Media;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace ClickLockNotifier;

internal sealed record SoundChoice(string Id, string DisplayName, string ResourceFileName);

internal sealed class SoundNotifier
{
    public const string DefaultSoundId = "correct-answer-tone";
    public const int DefaultVolumePercent = 80;
    private static readonly SoundChoice UnlockSound = new("unlock", "取消锁定", "akx-cloud-unlock.wav");

    private readonly Dictionary<string, byte[]> _soundBytesById = [];
    private readonly object _soundBytesLock = new();

    public static IReadOnlyList<SoundChoice> Choices { get; } =
    [
        new(DefaultSoundId, "确认音 1", "correct-answer-tone.wav"),
        new("interface-option-select", "确认音 2", "mixkit-interface-option-select-2573.wav"),
        new("software-interface-back", "确认音 3", "mixkit-software-interface-back-2575.wav"),
        new("correct-answer-notification", "确认音 4", "correct-answer-notification.wav"),
        new("sharp-notification", "确认音 5", "akx-sharp-notification.wav")
    ];

    public string SelectedSoundId { get; set; } = DefaultSoundId;

    public int VolumePercent { get; set; } = DefaultVolumePercent;

    public void Play()
    {
        var selectedChoice = Choices.FirstOrDefault(choice => choice.Id == SelectedSoundId)
            ?? Choices.First(choice => choice.Id == DefaultSoundId);
        Play(selectedChoice);
    }

    public void PlayUnlock()
    {
        Play(UnlockSound);
    }

    public void Preload()
    {
        _ = Task.Run(() =>
        {
            foreach (var choice in Choices.Append(UnlockSound))
            {
                try
                {
                    GetSoundBytes(choice);
                }
                catch
                {
                    // Playback has a system beep fallback, so preload failures are non-fatal.
                }
            }
        });
    }

    private void Play(SoundChoice selectedChoice)
    {
        _ = Task.Run(() =>
        {
            try
            {
                var soundBytes = GetSoundBytes(selectedChoice);
                var volume = Math.Clamp(VolumePercent, 0, 100) / 100f;
                using var stream = new MemoryStream(soundBytes);
                using var reader = new WaveFileReader(stream);
                using var output = new WaveOutEvent();
                using var completed = new ManualResetEventSlim();
                var volumeProvider = new VolumeSampleProvider(reader.ToSampleProvider())
                {
                    Volume = volume
                };

                output.PlaybackStopped += (_, _) => completed.Set();
                output.Init(volumeProvider);
                output.Play();
                completed.Wait();
            }
            catch
            {
                SystemSounds.Beep.Play();
            }
        });
    }

    private byte[] GetSoundBytes(SoundChoice choice)
    {
        lock (_soundBytesLock)
        {
            if (_soundBytesById.TryGetValue(choice.Id, out var cachedBytes))
            {
                return cachedBytes;
            }

            var resourceName = $"ClickLockNotifier.Assets.Sounds.{choice.ResourceFileName}";
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Missing embedded sound resource: {resourceName}");
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            var bytes = memory.ToArray();
            _soundBytesById[choice.Id] = bytes;
            return bytes;
        }
    }
}
