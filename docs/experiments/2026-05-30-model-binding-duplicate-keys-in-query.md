# 実験計画: Model Binding ラボ

## 0. メタ情報
- 実験名: Query 内同名キー重複の挙動
- 対象機能: ASP.NET Core MVC のモデルバインディング
- 作成日: 2026-05-30
- 担当: Me
- 対象ブランチ: 現在の作業ブランチ

## 1. 背景と目的
- 背景: Query 内で同じキーが複数来たとき、scalar パラメータがどの値を採用するかを確認したい。
- 確認したい仕様: `age=abc&age=42` のとき、先頭値で変換失敗した場合に 2 つ目へフォールバックするか。
- ゴール（何が分かれば完了か）: 同一ソース内重複キーでの scalar バインディングの採用ルールを説明できること。

## 2. 仮説
- 仮説 1: `?age=abc&age=42` では先頭値 `abc` を使って変換が試みられ、失敗時に 2 つ目 `42` へはフォールバックしない。

## 3. 前提条件
- .NET SDK バージョン: 9.0 系
- 実行環境（OS/ブラウザ）: Windows / 任意の Chromium 系ブラウザ
- DB 状態（初期化手順）: 不要
- 認証状態（未ログイン/ログイン済み）: どちらでも可

## 4. 変更内容（必要な場合）
- 追加/変更ファイル:
  - `src/AspNetCoreSandbox.Web/Controllers/HomeController.cs`
  - `src/AspNetCoreSandbox.Web/Views/Home/DuplicateInQuery.cshtml`
- 変更理由: Query 内同名重複キー時の採用値とエラーを 1 画面で観察できるようにするため。
- ロールバック手順: 追加した action と view を元に戻す。

## 5. 実験手順
1. アプリを起動し、`/Home/DuplicateInQuery?age=abc&age=42` を開く。
2. `Bound age` と `ModelState(age)` を確認する。
3. 次に `/Home/DuplicateInQuery?age=42&age=abc` を開き、結果の差を確認する。

### 5.1 リクエスト例
```http
GET /Home/DuplicateInQuery?age=abc&age=42 HTTP/1.1
Host: localhost:5001
```

### 5.2 期待結果
- 期待するステータスコード: 200
- 期待するレスポンス: 1 件目が無効値なら `Bound age` は null で `ModelState(age)` に変換エラー。1 件目が有効値ならその値でバインドされる。
- 期待するログ: 未処理例外が出ないこと。

## 6. 観察結果
- 実際のステータスコード: 200
- 実際のレスポンス: `?age=abc&age=42` のとき `Bound age=[(null)]` かつ `ModelState(age)` は "The value 'abc,42' is not valid."。`?age=42&age=abc` のとき `Bound age=[42]` かつエラーメッセージなし。
- 実際のログ: 未処理例外なし

## 7. 判定
- 仮説 1 の判定（採択/棄却）: 採択
- 判定理由: 1 件目が無効値（`abc`）のときはバインド失敗し、2 件目（`42`）へのフォールバックは起きなかった。一方、1 件目が有効値（`42`）ならその値で成功したため、先頭値優先の仮説と一致した。

## 8. 学びと次アクション
- 学び: Query の同名複数値は scalar バインド時に先頭値が実質的な採用候補となり、先頭値が変換不能でも後続値で再試行しない。変換失敗メッセージは複数値が連結された形（`abc,42`）で出る場合がある。
- 未解決事項: 同一ソース内重複キー時のエラーメッセージ組み立て（`abc,42` になる理由）の詳細実装は未追跡。
- 次にやること: `ValueProviderResult.FirstValue` と `Values.ToString()` の使い分けがエラーメッセージへ与える影響を、`SimpleTypeModelBinder` 起点で確認する。
