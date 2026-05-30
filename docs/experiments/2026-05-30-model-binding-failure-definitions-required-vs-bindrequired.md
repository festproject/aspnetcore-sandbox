# 実験計画: Model Binding ラボ

## 0. メタ情報
- 実験名: 失敗の定義（Required vs BindRequired）
- 対象機能: ASP.NET Core MVC のモデルバインディング
- 作成日: 2026-05-30
- 担当: Me
- 対象ブランチ: 現在の作業ブランチ

## 1. 背景と目的
- 背景: 「失敗」の意味が混ざりやすいため、未送信とバリデーション失敗を分離して確認したい。
- 確認したい仕様: `[Required]` と `[BindRequired]` で、キー未送信や空値送信時の ModelState がどう変わるか。
- ゴール（何が分かれば完了か）: 未送信と検証失敗を区別して説明できること。

## 2. 仮説
- 仮説 1: `[BindRequired]` はキー未送信時にバインド段階のエラーになる。
- 仮説 2: `[Required]` は値が null/空のとき検証エラーになる。

## 3. 前提条件
- .NET SDK バージョン: 9.0 系
- 実行環境（OS/ブラウザ）: Windows / 任意の Chromium 系ブラウザ
- DB 状態（初期化手順）: 不要
- 認証状態（未ログイン/ログイン済み）: どちらでも可

## 4. 変更内容（必要な場合）
- 追加/変更ファイル:
  - `src/AspNetCoreSandbox.Web/Controllers/HomeController.cs`
  - `src/AspNetCoreSandbox.Web/Models/FailureDefinitionsInput.cs`
  - `src/AspNetCoreSandbox.Web/Views/Home/FailureDefinitions.cshtml`
- 変更理由: 失敗種別を 1 画面で比較観察するため。
- ロールバック手順: 追加した action、model、view を元に戻す。

## 5. 実験手順
1. `/Home/FailureDefinitions` を開く。
2. `/Home/FailureDefinitions?RequiredOnly=ok` を開く。
3. `/Home/FailureDefinitions?BindRequiredOnly=ok` を開く。
4. `/Home/FailureDefinitions?RequiredOnly=&BindRequiredOnly=` を開く。
5. 各ケースの `ModelState(RequiredOnly)` と `ModelState(BindRequiredOnly)` を比較する。

### 5.1 リクエスト例
```http
GET /Home/FailureDefinitions?RequiredOnly=ok HTTP/1.1
Host: localhost:5001
```

### 5.2 期待結果
- 期待するステータスコード: 200
- 期待するレスポンス: `[BindRequired]` は未送信でエラー、`[Required]` は null/空でエラーになる。
- 期待するログ: 未処理例外が出ないこと。

## 6. 観察結果
- 実際のステータスコード: 200
- 実際のレスポンス: `[BindRequired]` は未送信でエラー、`[Required]` は null/空でエラーになる。
- 実際のログ: 未処理例外なし

## 7. 判定
- 仮説 1 の判定（採択/棄却）: 採択
- 仮説 2 の判定（採択/棄却）: 採択
- 判定理由: 実測で `[BindRequired]` はキー未送信時にエラーとなり、`[Required]` は null/空値に対して検証エラーとなった。未送信と検証失敗を区別できたため、仮説と一致する。

## 8. 学びと次アクション
- 学び: `[BindRequired]` はバインド段階の「値が来ていない」を検出し、`[Required]` は検証段階の「値が無効（null/空）」を検出する。
- 未解決事項: なし（今回の目的範囲では確認完了）。
- 次にやること: 必要になった時点で、型変換失敗（例: int に文字列）と `[BindRequired]` / `[Required]` の組み合わせケースを同形式で追加検証する。
