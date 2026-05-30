# 実験計画: Model Binding ラボ

## 0. メタ情報
- 実験名: Form 内同名キー重複の挙動
- 対象機能: ASP.NET Core MVC のモデルバインディング
- 作成日: 2026-05-30
- 担当: Me
- 対象ブランチ: 現在の作業ブランチ

## 1. 背景と目的
- 背景: Form 内で同じキーが複数来たとき、scalar パラメータがどの値を採用するかを確認したい。
- 確認したい仕様: `age=abc&age=42` のとき、先頭値で変換失敗した場合に 2 つ目へフォールバックするか。
- ゴール（何が分かれば完了か）: 同一ソース（Form）内重複キーでの scalar バインディング採用ルールを説明できること。

## 2. 仮説
- 仮説 1: `age=abc&age=42` では先頭値 `abc` で変換が試みられ、失敗時に 2 つ目 `42` へはフォールバックしない。

## 3. 前提条件
- .NET SDK バージョン: 9.0 系
- 実行環境（OS/ブラウザ）: Windows / 任意の Chromium 系ブラウザ
- DB 状態（初期化手順）: 不要
- 認証状態（未ログイン/ログイン済み）: どちらでも可

## 4. 変更内容（必要な場合）
- 追加/変更ファイル:
  - `src/AspNetCoreSandbox.Web/Controllers/HomeController.cs`
  - `src/AspNetCoreSandbox.Web/Views/Home/DuplicateInForm.cshtml`
- 変更理由: Form 内同名重複キー時の採用値とエラーを 1 画面で観察できるようにするため。
- ロールバック手順: 追加した action と view を元に戻す。

## 5. 実験手順
1. `/Home/DuplicateInForm` を開く。
2. Case A（`age=abc&age=42`）を送信し、`Bound age` と `ModelState(age)` を記録する。
3. Case B（`age=42&age=abc`）を送信し、結果の差を記録する。

### 5.1 リクエスト例
```http
POST /Home/DuplicateInForm HTTP/1.1
Host: localhost:5001
Content-Type: application/x-www-form-urlencoded

age=abc&age=42
```

### 5.2 期待結果
- 期待するステータスコード: 200
- 期待するレスポンス: 1 件目が無効値なら `Bound age` は null で `ModelState(age)` に変換エラー。1 件目が有効値ならその値でバインドされる。
- 期待するログ: 未処理例外が出ないこと。

## 6. 観察結果
- 実際のステータスコード: 200
- 実際のレスポンス: 1 件目が無効値なら `Bound age` は null で `ModelState(age)` に変換エラー。エラーメッセージは "The value 'abc,42' is not valid." 1 件目が有効値ならその値でバインドされる。
- 実際のログ: 未処理例外なし

## 7. 判定
- 仮説 1 の判定（採択/棄却）: 採択
- 判定理由: `age=abc&age=42` では `Bound age` が null となり、`ModelState(age)` は "The value 'abc,42' is not valid." になった。`age=42&age=abc` では `Bound age` が 42 でエラーなしだったため、先頭値優先で後続値へのフォールバックは起きないという仮説と一致した。

## 8. 学びと次アクション
- 学び: Form 内の同名複数値でも Query 実験と同様に、scalar バインドは先頭値の成否で結果が決まる。先頭値が無効な場合は `abc,42` のような連結表現を含む変換エラーになる。
- 未解決事項: エラーメッセージに複数値連結（`abc,42`）が出る詳細な文字列生成経路は未確認。
- 次にやること: 実装の深掘りは一旦止め、これまでの実験結果（Form > Route > Query、非フォールバック、同一ソース先頭値優先）を1ページに統合して優先順位・失敗ルール表を作る。
