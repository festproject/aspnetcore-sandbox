# 実験計画: AntiForgery ラボ

## 0. メタ情報
- 実験名: Missing token と Invalid token の失敗分解
- 対象機能: ASP.NET Core MVC AntiForgery
- 作成日: 2026-05-30
- 担当: Me
- 対象ブランチ: 現在の作業ブランチ

## 1. 背景と目的
- 背景: 400 になる条件が「トークン欠落」と「トークン不正」でどう違うかを明確にしたい。
- 確認したい仕様:
  1. request token 欠落時のメッセージ。
  2. request token 改ざん時のメッセージ。
  3. cookie token 欠落時のメッセージ。
- ゴール（何が分かれば完了か）: 失敗種別ごとの判定条件とメッセージ差分を再現可能に説明できること。

## 2. 仮説
- 仮説 1: request token 欠落では "request token was not provided" 系メッセージになる。
- 仮説 2: request token 改ざんでは "token was meant for a different request" 系メッセージになる。
- 仮説 3: cookie token 欠落では "cookie token must be provided" 系メッセージになる。

## 3. 前提条件
- .NET SDK バージョン: 9.0 系
- 実行環境（OS/ブラウザ）: Windows / Chromium 系ブラウザ
- DB 状態（初期化手順）: 不要
- 認証状態（未ログイン/ログイン済み）: どちらでも可

## 4. 変更内容（必要な場合）
- 追加/変更ファイル:
  - `src/AspNetCoreSandbox.Web/Controllers/HomeController.cs`
  - `src/AspNetCoreSandbox.Web/Views/Home/AntiForgeryFailureModes.cshtml`
  - `src/AspNetCoreSandbox.Web/Views/Home/Index.cshtml`
- 変更理由: 失敗モードを 1 画面で比較するため。
- ロールバック手順: 追加した action/view/link を元に戻す。

## 5. 実験手順
1. `/Home/AntiForgeryFailureModes` を開く。
2. `Valid baseline (form token)` を実行する。
3. `Missing request token` を実行する。
4. `Invalid request token` を実行する。
5. `Missing cookie token` を実行する。

### 5.1 リクエスト例
```http
POST /Home/AntiForgeryFailureModes HTTP/1.1
Host: localhost:7038
Content-Type: application/x-www-form-urlencoded

scenario=missing-request
```

### 5.2 期待結果
- 期待するステータスコード: valid は 200、missing/invalid 系は 400
- 期待するレスポンス: 失敗時は `message` に理由が入る JSON が返る。
- 期待するログ: 未処理例外が出ないこと。

## 6. 観察結果
- 実際のステータスコード:
  - valid-form: 200
  - missing-request: 400
  - invalid-request: 400
  - missing-cookie: 400
- 実際のレスポンス:
  - valid-form: `Validation succeeded.`
  - missing-request: `The required antiforgery request token was not provided in either form field "__RequestVerificationToken" or header value "RequestVerificationToken".`
  - invalid-request: `The antiforgery token could not be decrypted.`
  - missing-cookie: `The required antiforgery cookie ".AspNetCore.Antiforgery.DlX38oyl0aI" is not present.`
- 実際のログ: 未処理例外なし
- スクリーンショット/ログ保存先:
  - `docs/experiments/artifacts/2026-05-30-antiforgery-missing-vs-invalid-token/screenshots/localhost_7038_Home_AntiForgeryFailureModes_valid-form.png`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-missing-vs-invalid-token/screenshots/localhost_7038_Home_AntiForgeryFailureModes_missing-request.png`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-missing-vs-invalid-token/screenshots/localhost_7038_Home_AntiForgeryFailureModes_invalid-request.png`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-missing-vs-invalid-token/screenshots/localhost_7038_Home_AntiForgeryFailureModes_missing-cookie.png`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-missing-vs-invalid-token/logs/responsebody_valid-form.json`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-missing-vs-invalid-token/logs/responsebody_missing-request.json`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-missing-vs-invalid-token/logs/responsebody_invalid-request.json`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-missing-vs-invalid-token/logs/responsebody_missing-cookie.json`

## 7. 判定
- 仮説 1 の判定（採択/棄却）: 採択
- 仮説 2 の判定（採択/棄却）: 棄却
- 仮説 3 の判定（採択/棄却）: 採択
- 判定理由:
  - 仮説 1: request token 欠落時に期待どおり "not provided" 系メッセージだった。
  - 仮説 2: 改ざん token では "different request" ではなく "could not be decrypted" だった。
  - 仮説 3: cookie 欠落時に "cookie is not present" メッセージになった。

## 8. 学びと次アクション
- 学び:
  - request token 欠落と cookie 欠落は、それぞれ専用メッセージで切り分けられる。
  - token 改ざんの失敗理由は、内容によっては "could not be decrypted" になる。
  - 400 の内訳を分解すると、AntiForgery の失敗点をかなり正確に特定できる。
- 未解決事項: multipart/form-data での token 読取順序と失敗メッセージ。
- 次にやること: E3（検証タイミング）で Model Binding 前遮断を確認する。
