# UTAU Talk for YMM4

UTAU単独音音源を、ゆっくりMovieMaker4（YMM4）のキャラクターボイスとして使うプラグインです。[UtauTTS](https://github.com/yh2237/UtauTTS)の学習済みイントネーションとWORLDフレーズ合成を使用します。UtauTTSのGUIは常駐せず、セリフ生成時だけ同梱CLIを起動します。

## 対応環境

- Windows 10 / 11 x64
- ゆっくりMovieMaker4 v4.56.1.0以降
- UTAU単独音音源

音源は同梱していません。使用する音源の利用規約を確認してください。

## インストール

1. [Releases](https://github.com/YurKLYK/UtauTTSymm4/releases)から最新版の`UTAUTalkVoice-*.ymme`をダウンロードします。
2. YMM4を終了し、旧版の`UTAUTalkVoice-*`フォルダが`YukkuriMovieMaker_v4/user/plugin`に残っている場合はバックアップへ移します。同名DLLが複数あると新版が読み込まれません。
3. `.ymme`をダブルクリックしてインストールし、YMM4を起動します。
4. キャラクター設定の声質を`UTAU Talk / UTAU単独音`にし、`UTAU音源`に`character.txt`と`oto.ini`がある音源フォルダを指定します。

レンダラーの選択は不要です。同梱のUtauTTS v1.5.4を使用します。

## 調整

キャラクター設定で基本音程・話速・抑揚・アクセントを調整できます。音量はYMM4標準の設定を使います。キャラクターごとに別の音源を指定できます。

YMM4の「ツール」メニューから「UTAU Talk 抑揚」を開くと、一音ずつ調整するパネルが表示されます。YMM4のツールパネルなので、ドッキング・フロートして配置できます。

UTAU Talkのセリフを選ぶと、そのセリフの調整値を表示します。未解析なら自動で解析します。縦スライダーを上下に動かして音程を調整し、「試聴」で確認できます。100セントが半音、範囲は±1200セントです。

変更は少し待つと選択中のセリフへ保存されます。「YMM4へ適用」ですぐ保存できます。セリフを切り替えた際も保存されます。音声はYMM4側で再生成してください。本文を変更した場合は古い一音調整を外して解析し直します。

## ビルド

.NET 10 SDK、YMM4本体、公式UtauTTS v1.5.4 Windows x64配布物が必要です。

```powershell
.\packaging\build-release.ps1 `
  -Ymm4Directory 'C:\YukkuriMovieMaker_v4' `
  -UtauTtsReleaseDirectory 'C:\path\to\UtauTTS-win-x64-v1.5.4'
```

`dist/UTAUTalkVoice-0.2.0.ymme`が生成されます。ビルドスクリプトは日本語合成に必要なCLI・Open JTalk・v12モデル・WORLDランタイム・ライセンス文書のみを収録し、UTAU音源やUtauTTSのGUIは含めません。

## ライセンス

このプラグインのソースコードはMIT Licenseです。配布パッケージにはUtauTTS v1.5.4の一部と第三者コンポーネントを同梱します。各ライセンスと適用範囲は[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)および`.ymme`内`utautts.zip`の原文を確認してください。
