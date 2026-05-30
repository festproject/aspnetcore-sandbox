# 実験計画: Model Binding ラボ

## 0. メタ情報
- 実験名: バインド失敗時の非フォールバック
- 対象機能: ASP.NET Core MVC のモデルバインディング
- 作成日: 2026-05-30
- 担当: Me
- 対象ブランチ: 現在の作業ブランチ

## 1. 背景と目的
- 背景: Form / Route / Query の同名キー競合で、先頭ソースの値変換が失敗したときに次ソースへフォールバックするかを確認したい。
- 確認したい仕様: Form の `age=abc` で変換失敗しても、Route の `age=123` や Query の `age=456` に切り替わらないか。
- ゴール（何が分かれば完了か）: 変換失敗時にフォールバックしないことを 1 回の POST で説明できること。

## 2. 仮説
- 仮説 1: POST で Form の `age=abc` を送ると、Route/Query に有効な値があっても `age` のバインドは失敗し、ModelState エラーになる。

## 3. 前提条件
- .NET SDK バージョン: 9.0 系
- 実行環境（OS/ブラウザ）: Windows / 任意の Chromium 系ブラウザ
- DB 状態（初期化手順）: 不要
- 認証状態（未ログイン/ログイン済み）: どちらでも可

## 4. 変更内容（必要な場合）
- 追加/変更ファイル:
  - `src/AspNetCoreSandbox.Web/Controllers/HomeController.cs`
  - `src/AspNetCoreSandbox.Web/Views/Home/NoFallback.cshtml`
- 変更理由: 変換失敗時の非フォールバックを 1 画面で観察できるようにするため。
- ロールバック手順: 追加した action と view を元に戻す。

## 5. 実験手順
1. アプリを起動し、`/Home/NoFallback/123?age=456` を開く。
2. フォームの `age` は既定値 `abc` のまま送信する。
3. 画面の `Bound age` と `ModelState(age)` を確認し、Route/Query へのフォールバック有無を記録する。

### 5.1 リクエスト例
```http
POST /Home/NoFallback/123?age=456 HTTP/1.1
Host: localhost:5001
Content-Type: application/x-www-form-urlencoded

age=abc
```

### 5.2 期待結果
- 期待するステータスコード: 200
- 期待するレスポンス: `Bound age` は null のまま、`ModelState(age)` に変換エラーが出る。
- 期待するログ: 未処理例外が出ないこと。

## 6. 観察結果
- 実際のステータスコード: 200
- 実際のレスポンス: `Bound age` は null のまま。 `ModelState(age)` は "The value 'abc' is not valid." と表示される。
- 実際のログ: 未処理例外なし

## 7. 判定
- 仮説 1 の判定（採択/棄却）: 採択
- 判定理由: Form に `age=abc`、Route に `age=123`、Query に `age=456` を与えて POST した結果、`Bound age` は null のままで `ModelState(age)` に変換エラーが表示された。これは先頭で見つかった Form の値で変換失敗した後、Route/Query へフォールバックしなかったことを示す。

## 8. 学びと次アクション
- 学び: ValueProvider は文字列を返し、先頭ソースで取得した値を使って型変換する。変換失敗は ModelState エラーになるが、次ソースの値で再試行しない。
- 未解決事項: `ValueProviderResult.None` になる具体条件（キー未存在、空キー、複数値時の扱い）の詳細確認は未実施。
- 次にやること: 次回は「同一ソース内で同名キーが複数あるときの挙動」を 1 実験で確認し、先頭値固定と非フォールバックを検証する。
