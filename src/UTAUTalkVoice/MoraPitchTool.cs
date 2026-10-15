using System.ComponentModel;
using YukkuriMovieMaker.Plugin;

namespace UTAUTalkVoice;

public sealed class MoraPitchToolPlugin : IToolPlugin
{
    public string Name => "UTAU Talk 抑揚";
    public Type ViewModelType => typeof(MoraPitchToolViewModel);
    public Type ViewType => typeof(MoraPitchPanel);
    public bool AllowMultipleInstances => false;
}

public sealed class MoraPitchToolViewModel : IToolViewModel
{
    static MoraPitchToolViewModel? current;
    public MoraPitchToolViewModel() => current = this;

#pragma warning disable CS0067
    public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067
    public event EventHandler<CreateNewToolViewRequestedEventArgs>? CreateNewToolViewRequested;
    public string Title => "UTAU Talk 抑揚";
    public bool CanSuspend => true;

    public ToolState SaveState() => new() { Title = Title };
    public void LoadState(ToolState stateData) { }

    internal static bool RequestOpen()
    {
        if (MoraPitchPanel.Active is { } panel) {
            panel.BringIntoView();
            panel.Focus();
            return true;
        }
        if (current?.CreateNewToolViewRequested is null) return false;
        current.CreateNewToolViewRequested.Invoke(current,
            new CreateNewToolViewRequestedEventArgs(new ToolState { Title = current.Title }));
        return true;
    }
}

internal static class MoraPitchSelection
{
    static object? item;
    public static UTAUTalkVoiceParameter? Parameter { get; private set; }
    public static string Text { get; private set; } = "";
    public static event Action<UTAUTalkVoiceParameter?, string>? Changed;

    public static void Select(object selectedItem, UTAUTalkVoiceParameter? parameter, string text)
    {
        if (ReferenceEquals(item, selectedItem) && ReferenceEquals(Parameter, parameter) && Text == text) return;
        item = selectedItem;
        Parameter = parameter;
        Text = text;
        Changed?.Invoke(parameter, text);
    }

    public static void Clear(object? oldItem)
    {
        if (oldItem is null || !ReferenceEquals(item, oldItem)) return;
        item = null;
        Parameter = null;
        Text = "";
        Changed?.Invoke(null, "");
    }
}
