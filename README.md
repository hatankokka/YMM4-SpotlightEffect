# YMM4 SpotlightEffect

ゆっくりMovieMaker4 用の矩形スポットライト映像エフェクトプラグインです。
画像・動画の周囲を暗くし、指定した矩形範囲だけを明るく表示できます。

## 機能

- 画面全体を暗くする
- 指定した矩形範囲だけを元の明るさで表示する
- 中心X / 中心Y / 幅 / 高さを調整
- 暗くする強度を調整
- 境界ぼかしに対応

## 必要環境

- Windows
- ゆっくりMovieMaker4
- .NET SDK

## ビルド前の準備

`Directory.Build.props.sample` をコピーして、`Directory.Build.props` にリネームしてください。

その後、`Directory.Build.props` の中の `YMM4DirPath` を自分の YMM4 フォルダに書き換えます。

例:

```xml
<Project>
  <PropertyGroup>
    <YMM4DirPath>G:\ymm4\YukkuriMovieMaker_v4\</YMM4DirPath>
  </PropertyGroup>
</Project>
```

末尾の `\` を消さないでください。

## ビルド方法

PowerShell またはコマンドプロンプトで、このプロジェクトフォルダに移動してから実行します。

```powershell
dotnet build SpotlightEffect.csproj -c Release
```

YMM4 が起動中の場合、DLL がロックされてコピーに失敗することがあります。
ビルド前に YMM4 を完全に終了してください。

## インストール

ビルド後、生成された `SpotlightEffect.dll` を以下のようなフォルダに配置します。

```text
YukkuriMovieMaker_v4\user\plugin\SpotlightEffect\
```

プロジェクト設定でコピー処理を設定している場合は、ビルド時に自動コピーされます。

## 使い方

1. YMM4を起動します。
2. タイムライン上の画像または動画アイテムを選択します。
3. 右側パネルの「映像エフェクト」から「スポットライト」を追加します。
4. 中心X / 中心Y / 幅 / 高さ / 暗くする強度 / 境界ぼかし を調整します。

## 注意

このプラグインは非公式プラグインです。
ゆっくりMovieMaker4 本体および YMM4 関連DLLは同梱していません。

## License

MIT License
