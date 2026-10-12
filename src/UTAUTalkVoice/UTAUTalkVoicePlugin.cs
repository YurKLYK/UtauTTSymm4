using YukkuriMovieMaker.Plugin.Voice;

namespace UTAUTalkVoice;

public sealed class UTAUTalkVoicePlugin : IVoicePlugin
{
    public string Name => "UTAU Talk";
    public IEnumerable<IVoiceSpeaker> Voices => [new UTAUTalkVoiceSpeaker()];
    public bool CanUpdateVoices => false;
    public bool IsVoicesCached => true;
    public Task UpdateVoicesAsync() => Task.CompletedTask;
}
