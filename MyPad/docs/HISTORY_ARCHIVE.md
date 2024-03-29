## 変更履歴 (アーカイブ)

### v1.12.5

  [change]
  - 軽微な修正

### v1.12.4

  [update]
   - 一部の画面のレイアウトを調整

### v1.12.3

  [update]
  - コピーと切り取りの際に書式を保持するかどうかを制御できるように変更

### v1.12.2

  [fix]
  - 一部のメッセージボックス、ダイアログで Enter キー、Escape キー押下時の動作が統一されていない問題を修正

### v1.12.1

  [fix]
  - 一部の画面のレイアウトの微修正

### v1.12.0

  [change]
  - .NET 5 への移行

### v1.11.4

  [fix]
  - 空のドキュメントを含んだ状態で差分表示するとエラーになる不具合を修正

### v1.11.3

  [fix]
  - エクスプローラーのファイルアイコンのサイズが不定となる不具合を修正
  - オプション画面の一部設定の文言を修正

### v1.11.2

  [fix]
  - アクティブなハンバーガーメニューの項目の背景色が描画されない不具合を修正

### v1.11.1

  [update]
  - 一部の操作に対するショートカットキーを追加、変更

### v1.11.0

  [update]
  - システムメンテナンス用の画面を追加

### v1.10.5

  [fix]
  - Windows の「プログラムから開く」によってアプリケーションを起動した際にエラーが発生する不具合を修正
  - 項目を開いたままサイドバーを非表示にすると空白の領域が残る不具合を修正

### v1.10.4

  [update]
  - 拡張子による関連付けの対象を拡充

### v1.10.3

  [fix]
  - OSSライセンスのリンクを修正

### v1.10.2

  [fix]
  - ハンバーガーメニューの幅を変更するスプリッターが表示されない不具合を修正

### v1.10.1

  [update]
  - バージョン情報画面を整備

### v1.10.0

  [update]
  - ターミナルを追加
  - C#スクリプトランナーを追加

### v1.9.1

  [update]
  - ログ出力の仕様を変更

### v1.9.0

  [update]
  - コードの展開と折り畳みをコマンドメニューに追加
  - コードの展開と折り畳みをショートカットキーで実行できるように変更

### v1.8.4

  [fix]
  - 指定行へ移動するダイアログで、行番号を指定しなくても OK ボタンを押下できる不具合を修正

### v1.8.3

  [fix]
  - オプション画面からエクスプローラーへ適用を行う際、空行も反映されてしまう不具合を修正
  - 一部のメニューボタンにタブフォーカスが遷移してしまう不具合を修正

### v1.8.2

  [fix]
  - 設定の初期化が正常に動作しない不具合を修正
  - フォルダをドロップした際、ファイルを選ばずにダイアログをキャンセルするとエラーになる不具合を修正

### v1.8.1

  [update]
  - エクスプローラーのルートフォルダを指定できるように変更

  [fix]
  - エクスプローラーによるファイルの探索に失敗した際、アプリが強制終了してしまう不具合を修正

### v1.8.0

  [update]
  - ファイルツリーからシステムのエクスプローラーを開くための機能を追加
  - 全画面表示した際、全画面表示を解除するためのボタンを表示するように変更

  [fix]
  - キーワード補完が有効でかつ補完候補が無い状態では、Enter キーを二度押下しなければ改行できない不具合を修正

### v1.7.6

  [fix]
  - 画面領域を構築する際、稀に例外が発生する不具合への対策(試作)

### v1.7.5

  [update]
  - 設定項目に「タブ幅」を追加

  [fix]
  - 設定ファイルを開いている状態で、このファイルから設定をインポートした際、設定が反映されない不具合を修正

### v1.7.4

  [update]
  - JSONC 形式のシンタックスハイライトに対応
  - シンタックスハイライトに紐づく拡張子を追加

  [fix]
  - 一部のメッセージを修正

### v1.7.3

  [update]
  - システム設定のインポート、エクスポート、初期化を行う機能を追加
  - "指定行へ移動" の行番号の上下移動が反転されるように変更

### v1.7.2

  [fix]
  - 拡大率を調整した際、画面に表示されている行がずれてしまう不具合を修正

### v1.7.1

  [update]
  - 新規でかつ未変更のタブがアクティブの状態で、ファイルを読み込んだ場合、このタブにテキストがロードされるように変更

  [fix]
  - ファイルをリロードした際、マーカーがこれを変更として捉えてしまう不具合を修正
  - フォントサイズの設定値が正しく反映されない不具合を修正

### v1.7.0

  [update]
  - アプリケーションのアイコンを変更

  [fix]
  - 矩形選択時に選択範囲の長さを正しく計算できていない不具合を修正

### v1.6.6

  [update]
  - 一部のコマンドのテキストとショートカットキーを追加、変更
  - メニュー項目の表示を変更

  [fix]
  - 一部のシステムボタンにタブフォーカスが遷移してしまう不具合を修正

### v1.6.5

  [update]
  - 重要度の低いアプリ内のトースト通知の表示、非表示を制御できるように変更
  - 未変更のファイルは上書き保存しないように変更

### v1.6.4

  [update]
  - 目安線の表示機能を追加
  - オプション画面内の項目の配置を変更

### v1.6.3

  [update]
  - ツールバーにコマンドを追加
  - 画面内通知の大きさを変更

  [fix]
  - 英語設定で実行中に、選択した文字列の半角全角の変換に失敗する不具合を修正

### v1.6.2

  [update]
  - F6キーでテキストエリアからサイドバーコンテンツへフォーカスを遷移できるように変更

  [fix]
  - 変更行と折り畳み表示のマーカーの位置が入れ替わる場合がある不具合を修正

### v1.6.1

  [update]
  - 可視化された改行マークに使用される文字を変更(試作)

  [fix]
  - シンタックス定義を変更した際、テキストの再読み込みが行われた通知が表示される不具合を修正
  - 隠しフォルダではない状態で一時フォルダが生成される場合がある不具合を修正

### v1.6.0

  [update]
  - 変更行を示すマーカーを追加(試作)
  - アプリケーションの起動時に、残留する一時フォルダの存在を通知するように変更

### v1.5.3

  [update]
  - テキストを保存、または読み込んだ際の通知内容を変更
  - ステータスバーが非表示の際はリサイズグリップも非表示とするように変更

  [fix]
  - ファイルを再読み込みした際、折り畳み表示が適用されない不具合を修正

### v1.5.2

  [update]
  - 未処理の例外を検知した際、同時に複数のダイアログが表示されないように変更
  - バージョンアップ後に起動した際、バージョン情報画面を表示するように変更

  [fix]
  - ファイルを開く、保存するダイアログで、拡張子のフィルターが正常に機能しない不具合を修正
  - アプリ内のトースト通知を×ボタン以外を押して閉じた際、通知データが残留してしまう不具合を修正

### v1.5.1

  [update]
  - タブ追加ボタンのデザインを変更
  - タブ上のコンテキストメニューの項目を追加
  - ステータスバーの表記を変更

### v1.5.0

  [update]
  - エディターの折り畳み機能を追加

### v1.4.1

  [update]
  - テキストを読み込んだ際、通知を表示するように変更
  - スタイルや表示の微調整

### v1.4.0

  [update]
  - サイドバーを追加
  - エクスプローラーパネルを追加
  - ファイルプロパティ画面をサイドバーに統合

### v1.3.1

  [fix]
  - フライアウトを閉じた際、テキストエディタにフォーカスが戻らない不具合を修正
  - タブ移動で検索パネルからフォーカスが抜けてしまう不具合を修正

### v1.3.0

  [update]
  - ファイルプロパティ画面を追加

### v1.2.2

  [update]
  - Diff ビューアー画面でインライン表示への切り替えを行えるように変更

  [fix]
  - 検索パネルを初めて表示する際、検索テキスト欄にフォーカス遷移しない不具合を修正

### v1.2.1

  [update]
  - シンタックスハイライトの変更
  - 入力欄を持つダイアログを表示した際、入力欄にフォーカスが設定されるように変更

  [fix]
  - 通知領域アイコンの変更漏れを修正
  - 比較対象のファイルを選択する際、ファイルを指定しなくても OK ボタンを押下できる不具合を修正

### v1.2.0

  [update]
  - アプリケーションのアイコンを変更
  - 変更履歴をアプリケーション内で表示

### v1.1.3

  [update]
  - ログファイルの ZIP 出力
  - 印刷の実行後にプレビュー画面を閉じるように変更

### v1.1.2

  [update]
  - Diff ビューアーを表示した際、比較するテキストが同一であればオーバーレイを表示するように変更
  - 全画面表示の状態でメニューバーが表示されるように変更

### v1.1.1

  [update]
  - 拡張子との関連付けを定義
  - 免責事項を追加
  - 寄付のご案内を追加

  [fix]
  - '指定行へ移動' を実行した際、指定された行へスクロールしない不具合を修正
  - 印刷プレビュー画面において、空のドキュメントを示すオーバーレイが表示されない不具合を修正

### v1.1.0

  - 初版リリース