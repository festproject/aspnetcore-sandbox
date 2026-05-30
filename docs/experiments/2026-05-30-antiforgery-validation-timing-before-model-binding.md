# 実験計画: AntiForgery ラボ

## 0. メタ情報
- 実験名: AntiForgery 検証タイミング（Model Binding 前遮断）
- 対象機能: ASP.NET Core MVC AntiForgery
- 作成日: 2026-05-30
- 担当: Me
- 対象ブランチ: 現在の作業ブランチ

## 1. 背景と目的
- 背景: AntiForgery が Authorization フェーズで失敗した場合、Model Binding より前に 400 で遮断されるかを確認したい。
- 確認したい仕様:
  1. 保護エンドポイントで token 不正時に action が実行されないか。
  2. 同じ `age=abc` 入力でも、保護エンドポイント失敗時は binding error が出ないか。
  3. 非保護エンドポイントでは `age=abc` の binding error が通常どおり出るか。
- ゴール（何が分かれば完了か）: AntiForgery が Model Binding より前段で遮断することを再現可能に示せること。

## 2. 仮説
- 仮説 1: 保護 + token 不正/欠落では 400 となり、action 未到達になる。
- 仮説 2: 保護 + token 有効 + `age=abc` では action 到達し、`age` の binding error が観測できる。
- 仮説 3: 非保護 + `age=abc` では action 到達し、`age` の binding error が観測できる。

## 3. 前提条件
- .NET SDK バージョン: 9.0 系
- 実行環境（OS/ブラウザ）: Windows / Chromium 系ブラウザ
- DB 状態（初期化手順）: 不要
- 認証状態（未ログイン/ログイン済み）: どちらでも可

## 4. 変更内容（必要な場合）
- 追加/変更ファイル:
  - `src/AspNetCoreSandbox.Web/Controllers/HomeController.cs`
  - `src/AspNetCoreSandbox.Web/Views/Home/AntiForgeryBeforeModelBinding.cshtml`
  - `src/AspNetCoreSandbox.Web/Views/Home/Index.cshtml`
- 変更理由: AntiForgery 成否と Model Binding 実行有無を同一入力で比較するため。
- ロールバック手順: 追加した action/view/link を元に戻す。

## 5. 実験手順
1. `/Home/AntiForgeryBeforeModelBinding` を開く。
2. `Protected + valid token + age=abc` を実行する。
3. `Protected + missing token + age=abc` を実行する。
4. `Protected + invalid token + age=abc` を実行する。
5. `Unprotected + no token + age=abc` を実行する。

### 5.1 リクエスト例
```http
POST /Home/AntiForgeryBeforeModelBinding/protected HTTP/1.1
Host: localhost:7038
Content-Type: application/x-www-form-urlencoded

scenario=protected-missing&age=abc
```

### 5.2 期待結果
- 期待するステータスコード: protected-valid は 200、protected-missing/protected-invalid は 400、unprotected は 200
- 期待するレスポンス: 200 ケースは JSON で `ageErrors` が入る。400 ケースは action 未到達で `X-Action-Reached` が付かない。
- 期待するログ: 未処理例外が出ないこと。

## 6. 観察結果
- 実際のステータスコード:
  - protected-valid: 200
  - protected-missing: 400（スクリーンショットで確認）
  - protected-invalid: 400（スクリーンショットで確認）
  - unprotected: 200
- 実際のレスポンス:
  - protected-valid: `boundAge=(null)`, `ageErrors=["The value 'abc' is not valid."]`
  - unprotected: `boundAge=(null)`, `ageErrors=["The value 'abc' is not valid."]`
  - protected-missing/protected-invalid: action 未到達（`X-Action-Reached` ヘッダーなし）を画面表示で確認
- 実際のログ: 未処理例外なし
- スクリーンショット/ログ保存先:
  - `docs/experiments/artifacts/2026-05-30-antiforgery-validation-timing-before-model-binding/screenshots/localhost_7038_Home_AntiForgeryBeforeModelBinding_protected-valid.png`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-validation-timing-before-model-binding/screenshots/localhost_7038_Home_AntiForgeryBeforeModelBinding_protected-missing.png`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-validation-timing-before-model-binding/screenshots/localhost_7038_Home_AntiForgeryBeforeModelBinding_protected-invalid.png`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-validation-timing-before-model-binding/screenshots/localhost_7038_Home_AntiForgeryBeforeModelBinding_unprotected.png`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-validation-timing-before-model-binding/logs/responsebody_protected-valid.json`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-validation-timing-before-model-binding/logs/responsebody_unprotected.json`

## 7. 判定
- 仮説 1 の判定（採択/棄却）: 採択
- 仮説 2 の判定（採択/棄却）: 採択
- 仮説 3 の判定（採択/棄却）: 採択
- 判定理由:
  - protected-missing / protected-invalid は 400 かつ action 未到達で、AntiForgery 失敗が前段で遮断した。
  - protected-valid / unprotected はともに action 到達し、`age=abc` の binding error（`The value 'abc' is not valid.`）を返した。

## 8. 学びと次アクション
- 学び:
  - AntiForgery 失敗は authorization フェーズで 400 にし、action と model binding 実行を止める。
  - AntiForgery を通過した場合は、同じ入力 `age=abc` で通常の model binding error が観測される。
  - これにより AntiForgery 失敗と binding 失敗の責務境界が明確になった。
- 未解決事項: multipart/form-data でも同じタイミングで遮断されるか。
- 次にやること: E4（Body/multipart 境界）を実装する。
