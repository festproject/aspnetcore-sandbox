# AntiForgery Findings Summary (2026-05-30)

このドキュメントは、当日の AntiForgery 実験結果を 1 ページに統合したものです。

## 1. Request token の読取優先順位

結論: request token は form/header のどちらでも受理され、同時送信時は header が優先される。

根拠
- form-only は 200。
- header-only は 200。
- header-valid + form-invalid は 200。
- missing-both は 400。

## 2. 失敗種別（missing vs invalid）

結論: 400 は単一理由ではなく、欠落箇所・不正内容ごとにメッセージが分かれる。

根拠
- missing-request: `request token was not provided`。
- invalid-request: `The antiforgery token could not be decrypted.`
- missing-cookie: `cookie is not present`。

## 3. 検証タイミング（Model Binding 前遮断）

結論: 保護エンドポイントでは AntiForgery 失敗が先に評価され、action と model binding は実行されない。

根拠
- protected-missing / protected-invalid は 400 かつ action 未到達。
- protected-valid / unprotected は `age=abc` の model binding error を返した。

## 4. Body(JSON) と multipart の境界

結論: content-type で token 読取経路が分かれる。

根拠
- JSON + header token は 200。
- JSON + body field token only は 400。
- multipart + form token は 200。
- multipart + header token only は 200。
- multipart + no token は 400。

## 5. 実験一覧

- `2026-05-30-antiforgery-token-source-priority.md`
- `2026-05-30-antiforgery-missing-vs-invalid-token.md`
- `2026-05-30-antiforgery-validation-timing-before-model-binding.md`
- `2026-05-30-antiforgery-body-vs-multipart-boundary.md`
- `2026-06-03-antiforgery-signin-invalidation-and-username-casing.md`
- `2026-06-06-antiforgery-signinasync-user-propagation-and-cookie-unprotect.md`
- `2026-06-06-antiforgery-token-binding-with-path-based-auth-schemes.md`

## 6. 次にやるなら

- `DefaultAntiforgeryTokenStore.GetRequestTokensAsync` の form 読み取り抑制（header 先行時）を、multipart 大容量入力で計測する。
- `[AutoValidateAntiforgeryToken]` の controller 全体適用時の挙動を、個別 `[ValidateAntiForgeryToken]` と比較する。
