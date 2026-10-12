# Goアプリからの合成

このディレクトリは独立したGoモジュールです。`internal/`やQtを使わず、公開パッケージだけで音源の一覧取得・合成・WAV保存・終了を行います。

## 実行

Go 1.27以上と、UtauTTSの音源・モデル・Renderer・runtimeが必要です。

```sh
go mod tidy
go run . -assets /path/to/UtauTTS -out speech.wav
```

`-assets`には次の配置を指定します。

```text
UtauTTS/
  voice/       音源ディレクトリ
  models/      韻律モデル
  renderer/    Rendererのmanifest
  runtime/
    utautts-worldline-bridge[.exe]
    utautts-world-engine.dll またはOS用の共有ライブラリ
    utautts-openjtalk-features[.exe]
    open_jtalk_dic_utf_8-1.11/
```

開発環境でhelperや辞書が別の場所にある場合は、`-openjtalk-helper`と`-openjtalk-dictionary`で指定します。音源の配置は`-voice-dir`で変更できます。資源と音源の利用・再配布条件は、それぞれのライセンスに従ってください。

## APIの範囲

- `New(Config)`で配置を指定し、`Voicebanks`・`Renderers`・`Models`で一覧を取得します。
- `Analyze(ctx, Request)`は読みとモーラ、`Synthesize(ctx, Request)`はPCM音声・読み・音素時刻・診断を返します。
- `Options`の`nil`は既定値です。指定した`0`・`false`は明示値で、範囲外の値は`ErrInvalidRequest`になります。休止とリリースの`0`は長さゼロです。
- 共通設定は`Options`、方式固有の設定は`RendererSettings`へ指定します。同じ共通項目を両方に入れることはできません。
- 要求のcontextでキャンセル・タイムアウトを指定します。`ErrClosed`・`ErrUnavailable`・`ErrInvalidRequest`とcontextのエラーは`errors.Is`で判定できます。
- `Close`は実行中の要求をキャンセルして完了を待ち、そのEngineのキャッシュと常駐プロセスを解放します。`Reload`は要求完了を待って音源を再読込します。Renderer・モデルのカタログを更新する場合はEngineを作り直します。

音声の保存先は合成要求に含めません。`Result.Audio.Samples`を再生・送信に使うか、`WriteWAV(io.Writer)`で保存できます。音素時刻は計画から作るラベルで、音声の強制アライメントではありません。

## moduleパス

importパスは`github.com/yh2237/UtauTTS`です。この例は`replace github.com/yh2237/UtauTTS => ../..`で同じチェックアウトの本体を参照し、`v0.0.0`をローカル参照用の仮のバージョンとして使います。

v1.5.1以前のリリースには公開Go APIがありません。別のアプリでリモート取得する場合は、APIを含むコミットまたはリリースを指定してください。`@latest`で旧版を選ばないように注意してください。APIの互換性方針は[Go向け合成API](../../docs/go-api.md)に記載しています。
