---
title: インストール
description: デスクトップ版の入手、起動、更新
---

# インストール

デスクトップ版は、[GitHubのリリース](https://github.com/yh2237/UtauTTS/releases)からダウンロードしてください。インストーラーはありません。展開したフォルダを好きなところにおいてショートカットとか作って下さい。

| ファイル | 対応環境 |
| --- | --- |
| `UtauTTS-win-x64.zip` | Windows（x64） |
| `UtauTTS-mac-arm64.zip` | macOS（Apple Silicon） |
| `UtauTTS-linux-x64.zip` | Linux（x64） |

名前に`Server`が付くものは、HTTPで音声合成を使うためのServer版です（[UtauTTS Server](https://github.com/yh2237/UtauTTS/blob/main/docs/server.md)）。

展開したフォルダの中身は構成を変えずに使ってください。

## Windows

1. `UtauTTS-win-x64.zip`を好きなフォルダへ展開します。
2. 展開したフォルダの`utautts.exe`を起動します。

## macOS

macOS版はApple Silicon向けです。Appleの署名と公証を受けていないため、初回の起動時に警告が出ます。公式のGitHubリリースからダウンロードしたファイルであることを確かめてから実行して下さい。

```bash
cd "/path/to/extracted-folder"
xattr -rc "utautts.app" tools runtime
open "utautts.app"
```

`/path/to/extracted-folder`は、実際に展開したフォルダのパスに置き換えてください。

## Linux

Linux版は、システムにインストールしたQt 6.5以降（Qt Quick、Qt Quick Controls、Qt Multimedia）と日本語フォントを使います。Debian 13では、次のパッケージをインストールします。

```bash
sudo apt-get update
sudo apt-get install -y fontconfig fonts-noto-cjk \
  qt6-base-dev qt6-declarative-dev qt6-multimedia-dev \
  qml6-module-qtquick-controls qml6-module-qtmultimedia
```

ZIPを展開したら、必要に応じて実行権限を付けて起動します。

```bash
chmod +x utautts tools/* runtime/utautts-openjtalk-features runtime/utautts-worldline-bridge
./utautts
```

ほかのディストリビューションでは、同じ機能のQtパッケージをインストールしてください。

## 更新

新しいバージョンが出ると、起動時に新しいバージョンの通知が出るようになります。特にこだわりが無い限り更新してください。更新に失敗する場合は大抵UtauTTSのファイルを他のプロセスが掴んでいる場合が殆どだと思うので、エクスプローラとかを全部落としてもう一度試してください。
