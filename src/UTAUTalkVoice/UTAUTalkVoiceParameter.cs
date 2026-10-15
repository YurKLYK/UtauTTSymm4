using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Plugin.Voice;

namespace UTAUTalkVoice;

public sealed class UTAUTalkVoiceParameter : VoiceParameterBase
{
    string voicebankDirectory = "";
    int baseNote = 60;
    double speechRate = 1.0;
    double intonation = 1.0;
    double accentStrength = 1.0;
    string moraPitch = "";

    [Display(Name = "一音ずつの抑揚", Description = "セリフを解析し、各モーラの高さを調整します。100セント＝半音")]
    [MoraPitchEditor]
    public string MoraPitch { get => moraPitch; set => Set(ref moraPitch, value); }

    [Display(Name = "UTAU音源", Description = "character.txtとoto.iniのある音源フォルダ")]
    [DirectorySelector]
    public string VoicebankDirectory
    {
        get => voicebankDirectory;
        set => Set(ref voicebankDirectory, value);
    }

    [Display(Name = "基本音程", Description = "MIDIノート番号。60はC4")]
    [TextBoxSlider("F0", "", 36, 84, Delay = -1)]
    [Range(36, 84)]
    [DefaultValue(60)]
    public int BaseNote { get => baseNote; set => Set(ref baseNote, value); }

    [Display(Name = "話速", Description = "1.0が標準")]
    [TextBoxSlider("F2", "倍", 0.25, 3.0, Delay = -1)]
    [Range(0.25, 3.0)]
    [DefaultValue(1.0)]
    public double SpeechRate { get => speechRate; set => Set(ref speechRate, value); }

    [Display(Name = "抑揚", Description = "文章全体の音程変化")]
    [TextBoxSlider("F2", "", 0.0, 2.0, Delay = -1)]
    [Range(0.0, 2.0)]
    [DefaultValue(1.0)]
    public double Intonation { get => intonation; set => Set(ref intonation, value); }

    [Display(Name = "アクセント", Description = "アクセント核による音程差")]
    [TextBoxSlider("F2", "", 0.0, 2.0, Delay = -1)]
    [Range(0.0, 2.0)]
    [DefaultValue(1.0)]
    public double AccentStrength { get => accentStrength; set => Set(ref accentStrength, value); }

}
