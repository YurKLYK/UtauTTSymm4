# UtauTTS for YMM4

UTAU音源を、ゆっくりMovieMaker4（YMM4）のキャラクターボイスとして利用するための音声合成プラグインです。

[UtauTTS](https://github.com/yh2237/UtauTTS)の学習済みイントネーションとWORLDフレーズ合成を使用します。UtauTTSのGUIを常駐させず、セリフ生成時だけ同梱CLIを起動します。

## 対応環境

- Windows 10 / 11 x64
- ゆっくりMovieMaker4 v4.56.1.0以降
- UTAU単独音音源

UTAU音源は同梱していません。各音源の利用規約を確認して使用してください。

## インストール

1. [Releases](https://github.com/YurKLYK/UtauTTSymm4/releases)から最新の`UTAUTalkVoice-*.ymme`をダウンロードします。
2. YMM4を終了します。
3. `.ymme`をダブルクリックしてインストールします。
4. YMM4のキャラクター設定で、声質を`UTAU Talk / UTAU単独音`にします。
5. `UTAU音源`へ`character.txt`と`oto.ini`がある音源フォルダを指定します。

レンダラーの選択は不要です。プラグインに同梱したUtauTTS v1.5.1を常に使用します。

## 設定

- 基本音程: 60でC4
- 話速: 1.0が標準（標準モーラ長130ms）
- 抑揚: UtauTTSの学習済みピッチ変化の強さ
- アクセント: 抑揚への倍率
- 音量: YMM4標準の音量設定を使用

設定はYMM4のキャラクターごとに保存されるため、キャラクターごとに異なるUTAU音源を利用できます。

## ビルド

.NET 10 SDK、YMM4本体、公式UtauTTS v1.5.1 Windows x64配布物が必要です。

```powershell
.\packaging\build-release.ps1 `
  -Ymm4Directory 'C:\YukkuriMovieMaker_v4' `
  -UtauTtsReleaseDirectory 'C:\path\to\UtauTTS-win-x64-v1.5.1'
```

`dist/UTAUTalkVoice-0.1.5.ymme`が生成されます。ビルドスクリプトはUtauTTS配布物から、日本語合成に必要なCLI・Open JTalk・v12モデル・WORLDランタイム・ライセンス文書だけを収録します。UTAU音源やUtauTTS GUIは収録しません。

## ライセンス

このプラグインのソースコードはMIT Licenseです。

配布パッケージにはUtauTTS v1.5.1の一部と、その第三者コンポーネントを同梱します。適用範囲や各ライセンスは[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)および`.ymme`内の`utautts.zip`に保存された原文を確認してください。
