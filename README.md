# MyPad

<div>
  <a href="https://www.microsoft.com/store/apps/9pp2600zm2jd">
    <img src="https://img.shields.io/badge/-Microsoft Store-80397B.svg?logo=microsoft&style=flat-square" alt="Store">
  </a>
  <a href="https://github.com/kawasawa/MyPad/releases">
    <img src="https://img.shields.io/github/release/kawasawa/MyPad.svg?style=flat-square" alt="Releases"/>
  </a>
  <a href="https://github.com/kawasawa/MyPad/blob/master/LICENSE.txt">
    <img src="https://img.shields.io/github/license/kawasawa/MyPad.svg?style=flat-square" alt="License">
  </a>
</div>

## 概要

MyPad は簡単操作ですぐに使えるシンプルなテキストエディタです。  
視認性に優れるモダンな外観に、使い慣れたクラシックな操作性を兼ね備え、様々な用途にご利用頂けます。

![mypad](./.images/mypad.jpg)

### 標準のメモ帳では足りないちょっとした機能

- 複数回使用可能な「元に戻す」「やり直し」
- 大小文字の区別や、正規表現による「検索」「置換」
- タブ切替による複数ファイルの表示と編集
- 行番号、空白、TAB、改行の可視化、折り返し表示
- 変更箇所の比較や差分検出

### 簡単なコーディングにも

- 文字コードとプログラミング言語の自動認識
- プログラミング言語別のカラー表示とキーワード補完
- ソースコードの折り畳み表示

### お好みにカスタマイズ

- テーマカラーや表示言語、フォントなど見た目の変更
- 入力を補助するためのエディタの動作制御
- 設定のインポート、エクスポート

![mypad](./.images/mypad-option.jpg)

## 開発情報

### 使用技術

本プログラムは以下を主な基盤として使用し、構築されています。

|                        | 使用技術               | Ver. (Minor) |
| :--------------------- | :--------------------- | -----------: |
| プログラミング言語     | C#                     |         10.0 |
| フレームワーク         | .NET                   |          6.0 |
| UI プラットフォーム    | WPF                    |            - |
| MVVM / DI インフラ     | Prism.Unity            |          8.1 |
| デザインテンプレート   | MahApps.Metro          |          2.4 |
| エディタコンポーネント | ICSharpCode.AvalonEdit |          6.1 |

### 構造

本プログラムの構造を以下に抜粋して示します。

```:
□ MyPad
│
├ $Extensions // 拡張メソッド
│
├ PubSub      // 非同期メッセージ
│
├ Models      // Model層のプログラム
│
├ ViewModels  // ViewModel層のプログラム
│ ├ Dialogs   // View.Dialogs に対応する ViewModel
│ └ Regions   // View.Regions に対応する ViewModel
│
└ Views       // View層のプログラム
  ├ Behaviors // ビヘイビアやトリガーアクション
  ├ Controls  // カスタムコントロール
  ├ Dialogs   // メッセージボックスやダイアログ
  ├ Markup    // マークアップ拡張機能
  ├ Regions   // リージョンコンテンツ
  └ Styles    // リソースディクショナリ
```

![dependencies](./.images/dependencies.drawio.png)
