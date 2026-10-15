using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using YukkuriMovieMaker.Commons;

namespace UTAUTalkVoice;

public sealed class MoraPitchEditorAttribute : PropertyEditorAttribute
{
    public override FrameworkElement Create() => new MoraPitchEditor();

    public override void SetBindings(FrameworkElement control, object item, object propertyOwner, PropertyInfo propertyInfo)
    {
        var editor = (MoraPitchEditor)control;
        editor.Bind(item, propertyOwner as UTAUTalkVoiceParameter);
        editor.SetBinding(MoraPitchEditor.ValueProperty, new Binding(propertyInfo.Name) {
            Source = propertyOwner, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        });
    }

    public override void ClearBindings(FrameworkElement control)
    {
        BindingOperations.ClearAllBindings(control);
        ((MoraPitchEditor)control).Unbind();
    }
}

public sealed class MoraPitchEditor : UserControl, IPropertyEditorControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(MoraPitchEditor), new FrameworkPropertyMetadata(""));
    public string Value { get => (string)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public UTAUTalkVoiceParameter? Parameter { get; private set; }
    public object? Item { get; private set; }
    public event EventHandler? BeginEdit;
    public event EventHandler? EndEdit;

    static MoraPitchEditor? activeEditor;
    readonly Button button;
    INotifyPropertyChanged? observedItem;

    public MoraPitchEditor()
    {
        button = new Button { Content = "抑揚パネルを表示…", Padding = new Thickness(8, 3, 8, 3) };
        button.Click += (_, _) => OpenEditor();
        Content = button;
    }

    public void SetFocus() => button.Focus();

    internal void Bind(object item, UTAUTalkVoiceParameter? parameter)
    {
        if (observedItem is not null) observedItem.PropertyChanged -= ItemChanged;
        Item = item;
        Parameter = parameter;
        observedItem = item as INotifyPropertyChanged;
        if (observedItem is not null) observedItem.PropertyChanged += ItemChanged;
        MoraPitchSelection.Select(item, parameter, Serif());
        activeEditor = this;
    }

    internal void Unbind()
    {
        if (observedItem is not null) observedItem.PropertyChanged -= ItemChanged;
        observedItem = null;
        var oldItem = Item;
        Item = null;
        Parameter = null;
        // YMM4 commonly clears and rebinds the same editor in one dispatcher turn.
        Dispatcher.BeginInvoke(DispatcherPriority.Background, () => {
            if (ReferenceEquals(activeEditor, this) && Parameter is null) {
                activeEditor = null;
                MoraPitchSelection.Clear(oldItem);
            }
        });
    }

    void ItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if ((string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Serif") && ReferenceEquals(sender, Item))
            Dispatcher.BeginInvoke(DispatcherPriority.Background, () => {
                if (ReferenceEquals(activeEditor, this) && Item is not null)
                    MoraPitchSelection.Select(Item, Parameter, Serif());
            });
    }

    string Serif() => Item?.GetType().GetProperty("Serif")?.GetValue(Item)?.ToString() ?? "";

    void OpenEditor()
    {
        if (Parameter is null) return;
        activeEditor = this;
        MoraPitchSelection.Select(Item!, Parameter, Serif());
        if (!MoraPitchToolViewModel.RequestOpen())
            MessageBox.Show(Window.GetWindow(this), "YMM4のツールメニューから「UTAU Talk 抑揚」を開いてください。\n一度開けば、セリフを選ぶたびに調整内容が切り替わります。",
                "UTAU Talk", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    internal static void ApplyTo(UTAUTalkVoiceParameter target, string value)
    {
        var editor = activeEditor;
        if (ReferenceEquals(editor?.Parameter, target)) editor?.BeginEdit?.Invoke(editor, EventArgs.Empty);
        target.MoraPitch = value;
        if (ReferenceEquals(editor?.Parameter, target)) editor?.EndEdit?.Invoke(editor, EventArgs.Empty);
    }
}

public sealed class MoraPitchPanel : UserControl
{
    internal static MoraPitchPanel? Active { get; private set; }
    UTAUTalkVoiceParameter? parameter;
    readonly TextBox text;
    readonly StackPanel moraSliders;
    readonly List<TextBox> pitchInputs = [];
    readonly TextBlock status;
    readonly Button analyze;
    readonly Button apply;
    readonly Button preview;
    readonly Button clear;
    readonly MediaPlayer player = new();
    readonly DispatcherTimer saveTimer = new() { Interval = TimeSpan.FromMilliseconds(400) };
    readonly string previewDirectory = Path.Combine(Path.GetTempPath(), "UTAUTalk-listen-" + Guid.NewGuid().ToString("N"));
    bool busy;
    bool unloaded;
    bool pendingAnalysis;
    bool suppressSave;
    int selectionRevision;
    int previewRevision;
    string? previewFile;
    MoraPitchDocument? document;
    public string Result { get; private set; } = "";

    public MoraPitchPanel()
    {
        PitchEditorTheme.Apply(this);

        var panel = new DockPanel { Margin = new Thickness(14), Background = Background };
        Content = panel;
        var top = new StackPanel(); DockPanel.SetDock(top, Dock.Top); panel.Children.Add(top);
        top.Children.Add(new TextBlock { Text = "一音ずつの抑揚", FontSize = 15, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
        top.Children.Add(new TextBlock { Text = "UTAU Talkのセリフを選ぶと、ここに解析結果が表示されます。", Opacity = .7 });
        text = new TextBox { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, Height = 48, Margin = new Thickness(0, 10, 0, 8),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto, ToolTip = "文章はYMM4のセリフ欄で編集してください" };
        top.Children.Add(text);
        var toolbar = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        top.Children.Add(toolbar);
        analyze = new Button { Content = "再解析", Margin = new Thickness(0, 0, 6, 0), IsEnabled = false };
        analyze.Click += (_, _) => RequestAnalysis(); toolbar.Children.Add(analyze);
        preview = new Button { Content = "▶ 試聴", Margin = new Thickness(0, 0, 6, 0), IsEnabled = false };
        preview.Click += async (_, _) => await PreviewAsync(); toolbar.Children.Add(preview);
        var stop = new Button { Content = "■ 停止" };
        toolbar.Children.Add(stop);

        var bottom = new StackPanel(); DockPanel.SetDock(bottom, Dock.Bottom); panel.Children.Add(bottom);
        status = new TextBlock { Text = "UTAU Talkのセリフを選択してください。", Opacity = .8,
            Margin = new Thickness(0, 8, 0, 8), TextWrapping = TextWrapping.Wrap };
        bottom.Children.Add(status);
        stop.Click += (_, _) => { previewRevision++; player.Stop(); status.Text = "停止しました。"; };
        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        bottom.Children.Add(actions);
        apply = new Button { Content = "YMM4へ適用", IsEnabled = false };
        apply.Click += (_, _) => SaveCurrent(); actions.Children.Add(apply);
        clear = new Button { Content = "調整を解除", IsEnabled = false, Margin = new Thickness(6, 0, 0, 0) };
        clear.Click += (_, _) => {
            if (busy || parameter is null) return;
            var target = parameter;
            saveTimer.Stop();
            suppressSave = true;
            Result = ""; MoraPitchEditor.ApplyTo(target, Result);
            if (document is not null) { foreach (var point in document.Points) point.Cents = 0; ShowSliders(); }
            status.Text = "調整を解除しました。";
        }; actions.Children.Add(clear);

        moraSliders = new StackPanel { Orientation = Orientation.Horizontal };
        panel.Children.Add(new ScrollViewer {
            Content = moraSliders, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Background = new SolidColorBrush(Color.FromRgb(42, 42, 42)),
        });
        saveTimer.Tick += (_, _) => { saveTimer.Stop(); SaveCurrent(false); };
        Loaded += (_, _) => {
            unloaded = false;
            Active = this;
            MoraPitchSelection.Changed += SelectionChanged;
            Select(MoraPitchSelection.Parameter, MoraPitchSelection.Text);
        };
        Unloaded += (_, _) => {
            SaveCurrent(false);
            unloaded = true;
            selectionRevision++;
            previewRevision++;
            MoraPitchSelection.Changed -= SelectionChanged;
            if (ReferenceEquals(Active, this)) Active = null;
            saveTimer.Stop(); player.Close();
            if (!busy) CleanupPreview();
        };
        player.MediaFailed += (_, e) => status.Text = "再生に失敗しました: " + e.ErrorException.Message;
    }

    void SelectionChanged(UTAUTalkVoiceParameter? selected, string sourceText) => Select(selected, sourceText);

    public void Select(UTAUTalkVoiceParameter? selected, string sourceText)
    {
        if (unloaded || (ReferenceEquals(parameter, selected) && text.Text == sourceText)) return;
        saveTimer.Stop();
        SaveCurrent(false);
        pendingAnalysis = false;
        suppressSave = false;
        selectionRevision++;
        previewRevision++;
        player.Stop();
        parameter = selected;
        text.Text = sourceText;
        document = null;
        Result = "";
        ShowSliders();
        if (selected is null) { status.Text = "UTAU Talkのセリフを選択してください。"; SetBusy(busy); return; }
        if (string.IsNullOrWhiteSpace(sourceText)) { status.Text = "セリフを入力すると自動で解析します。"; SetBusy(busy); return; }
        if (!string.IsNullOrWhiteSpace(selected.MoraPitch)) {
            try {
                var saved = JsonSerializer.Deserialize<MoraPitchDocument>(selected.MoraPitch);
                if (saved is not null && saved.Text == sourceText) {
                    document = saved;
                    ShowSliders();
                    status.Text = $"{saved.Points.Count}音。保存済みの調整を読み込みました。100セント＝半音。";
                    SetBusy(busy);
                    return;
                }
            } catch (JsonException) { /* Invalid or old data is replaced by a fresh analysis. */ }
            // Pitch points from a different sentence must not reach the renderer.
            MoraPitchEditor.ApplyTo(selected, "");
        }
        RequestAnalysis();
    }

    void ShowSliders()
    {
        moraSliders.Children.Clear(); pitchInputs.Clear();
        if (document is null) return;
        foreach (var point in document.Points)
        {
            var column = new StackPanel { Width = 72, Margin = new Thickness(2, 8, 2, 8) };
            column.Children.Add(new TextBlock { Text = point.Mora, FontSize = 17, FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center });
            column.Children.Add(new TextBlock { Text = $"{point.Position + 1}", FontSize = 11, Opacity = .65,
                HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 4) });
            var track = new Grid { Height = 150 };
            track.Children.Add(new Border { Height = 1, Background = Brushes.Gray, Opacity = .5,
                VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false });
            var slider = new Slider { Orientation = Orientation.Vertical, Minimum = -1200, Maximum = 1200,
                Value = Math.Clamp(point.Cents, -1200, 1200), Height = 150, TickFrequency = 100,
                SmallChange = 10, LargeChange = 100, IsSnapToTickEnabled = false,
                ToolTip = $"{point.Mora}: ±1200セント。ダブルクリックで0に戻す" };
            slider.ValueChanged += (_, _) => { point.Cents = slider.Value; QueueSave(); };
            slider.MouseDoubleClick += (_, e) => { slider.Value = 0; e.Handled = true; };
            track.Children.Add(slider); column.Children.Add(track);
            var input = new TextBox { TextAlignment = TextAlignment.Center, Margin = new Thickness(2, 6, 2, 2),
                ToolTip = "セントで直接入力（-1200～1200）" };
            input.SetBinding(TextBox.TextProperty, new Binding(nameof(Slider.Value)) {
                Source = slider, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
                StringFormat = "F0", ValidatesOnExceptions = true,
            });
            pitchInputs.Add(input); column.Children.Add(input);
            var reset = new Button { Content = "0に戻す", Margin = new Thickness(2, 3, 2, 0) };
            reset.Click += (_, _) => slider.Value = 0;
            column.Children.Add(reset);
            moraSliders.Children.Add(column);
        }
    }

    void QueueSave()
    {
        if (document is null || parameter is null) return;
        suppressSave = false;
        saveTimer.Stop(); saveTimer.Start();
    }

    void RequestAnalysis()
    {
        if (parameter is null || string.IsNullOrWhiteSpace(text.Text)) return;
        if (busy) { pendingAnalysis = true; status.Text = "選択したセリフを解析待ち…"; return; }
        _ = AnalyzeAsync();
    }

    async Task AnalyzeAsync()
    {
        if (parameter is null) return;
        var target = parameter;
        var source = text.Text;
        var revision = selectionRevision;
        var directory = Path.Combine(Path.GetTempPath(), "UTAUTalk-edit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        SetBusy(true);
        status.Text = "セリフを解析中…";
        try {
            var snapshot = new UTAUTalkVoiceParameter { VoicebankDirectory = target.VoicebankDirectory,
                BaseNote = target.BaseNote, SpeechRate = target.SpeechRate,
                Intonation = target.Intonation, AccentStrength = target.AccentStrength };
            var plan = Path.Combine(directory, "plan.json");
            await new UTAUTalkVoiceSpeaker().RenderAsync(source, snapshot, Path.Combine(directory, "preview.wav"), plan);
            if (unloaded || revision != selectionRevision || !ReferenceEquals(parameter, target)) return;
            document = MoraPitchDocument.FromPlan(source, await File.ReadAllTextAsync(plan));
            ShowSliders();
            status.Text = $"{document.Points.Count}音。上下に動かして高さを調整。100セント＝半音。";
        } catch (Exception ex) {
            if (!unloaded && revision == selectionRevision) status.Text = ex.Message;
        } finally {
            SetBusy(false);
            try { Directory.Delete(directory, true); } catch (IOException) { }
            if (unloaded) CleanupPreview();
            if (pendingAnalysis && !unloaded) { pendingAnalysis = false; RequestAnalysis(); }
        }
    }

    bool ValidateEdits()
    {
        if (parameter is null || document is null || document.Text != text.Text) return false;
        foreach (var input in pitchInputs) input.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        if (pitchInputs.Any(Validation.GetHasError)) { status.Text = "音程補正には数値を入力してください。"; return false; }
        try { _ = document.ManualPitchJson(); return true; }
        catch (Exception ex) { status.Text = ex.Message; return false; }
    }

    void SaveCurrent(bool showStatus = true)
    {
        if (suppressSave || !ValidateEdits()) return;
        Result = JsonSerializer.Serialize(document);
        if (parameter!.MoraPitch != Result) MoraPitchEditor.ApplyTo(parameter, Result);
        if (showStatus) status.Text = "このセリフに保存しました。";
    }

    UTAUTalkVoiceParameter Snapshot() => new() { VoicebankDirectory = parameter!.VoicebankDirectory,
        BaseNote = parameter.BaseNote, SpeechRate = parameter.SpeechRate,
        Intonation = parameter.Intonation, AccentStrength = parameter.AccentStrength,
        MoraPitch = JsonSerializer.Serialize(document) };

    async Task PreviewAsync()
    {
        if (busy || !ValidateEdits()) return;
        var snapshot = Snapshot();
        var revision = ++previewRevision;
        var selectedRevision = selectionRevision;
        var source = document!.Text;
        player.Close();
        if (previewFile is not null) { try { File.Delete(previewFile); } catch (IOException) { } }
        Directory.CreateDirectory(previewDirectory);
        previewFile = Path.Combine(previewDirectory, Guid.NewGuid().ToString("N") + ".wav");
        SetBusy(true); status.Text = "調整した声を生成中…";
        try {
            await new UTAUTalkVoiceSpeaker().RenderAsync(source, snapshot, previewFile);
            if (!unloaded && revision == previewRevision && selectedRevision == selectionRevision) {
                player.Open(new Uri(previewFile)); player.Play(); status.Text = "試聴中。変更したらもう一度「試聴」を押してください。";
            }
        } catch (Exception ex) { if (!unloaded && selectedRevision == selectionRevision) status.Text = ex.Message; }
        finally { SetBusy(false); if (unloaded) CleanupPreview();
            if (pendingAnalysis && !unloaded) { pendingAnalysis = false; RequestAnalysis(); } }
    }

    void SetBusy(bool value)
    {
        busy = value;
        analyze.IsEnabled = !value && parameter is not null && !string.IsNullOrWhiteSpace(text.Text);
        apply.IsEnabled = preview.IsEnabled = !value && document is not null;
        clear.IsEnabled = !value && parameter is not null;
    }

    void CleanupPreview()
    {
        try { if (Directory.Exists(previewDirectory)) Directory.Delete(previewDirectory, true); }
        catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}
