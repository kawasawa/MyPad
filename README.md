# MyPad

<div>
  <a href="https://www.microsoft.com/ja-jp/p/mypad-%E3%83%86%E3%82%AD%E3%82%B9%E3%83%88%E3%82%A8%E3%83%87%E3%82%A3%E3%82%BF/9pp2600zm2jd">
    <img src="https://img.shields.io/badge/-Microsoft Store-017ACC.svg?logo=microsoft&style=flat-square">
  </a>
  <a href="https://github.com/kawasawa/MyPad/blob/master/LICENSE.txt">
    <img src="https://img.shields.io/github/license/kawasawa/MyPad.svg?style=flat-square">
  </a>
</div>

## 概要

MyPad は簡単操作ですぐに使えるシンプルなテキストエディタです。  
視認性に優れるモダンな外観に、使い慣れたクラシックな操作性を兼ね備え、様々な用途にご利用頂けます。

### 標準のメモ帳では足りないちょっとした機能

- 複数回使用可能な [元に戻す] [やり直し]
- 大小文字の区別や、正規表現による [検索] [置換]
- タブ切替による複数ファイルの表示と編集
- 行番号、空白、TAB、改行の可視化、折り返し表示
- 変更箇所の比較や差分検出

### 簡単なコーディングにも

- 文字コードとプログラミング言語の自動認識
- プログラミング言語別のカラー表示とキーワード補完
- ソースコードの折り畳み表示

![mypad](./images/mypad.jpg)

## 開発情報

本プログラムは以下を主な基盤として採用し、構築されています。  

| 構成要素             | 採用項目                  | Ver.   |
|----------------------|---------------------------|--------|
| UI プラットフォーム  | WPF                       | -      |
| プログラミング言語   | C#                        | 8.0    |
| フレームワーク       | .NET Core                 | 3.1    |
| MVVM インフラ        | Prism                     | 8.0    |
| デザインテンプレート | MahApps.Metro             | 2.4    |
| コンポーネント       | ICSharpCode.AvalonEdit    | 6.0    |
