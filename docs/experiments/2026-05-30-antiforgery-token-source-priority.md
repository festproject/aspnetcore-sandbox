# 実験計画: AntiForgery ラボ

## 0. メタ情報
- 実験名: Request token 読取優先順位（Header vs Form）
- 対象機能: ASP.NET Core MVC AntiForgery
- 作成日: 2026-05-30
- 担当: Me
- 対象ブランチ: 現在の作業ブランチ

## 1. 背景と目的
- 背景: AntiForgery の request token は header と form どちらから読むかを確認したい。
- 確認したい仕様:
  1. form token のみで検証に通るか。
  2. header token のみで検証に通るか。
  3. header と form を同時送信したとき、どちらが優先されるか。
- ゴール（何が分かれば完了か）: header/form の優先順位を再現可能な手順で説明できること。

## 2. 仮説
- 仮説 1: form token のみでも POST は成功する。
- 仮説 2: header token のみでも POST は成功する。
- 仮説 3: header 有効 + form 無効の同時送信では header が優先され、POST は成功する。

## 3. 前提条件
- .NET SDK バージョン: 9.0 系
- 実行環境（OS/ブラウザ）: Windows / Chromium 系ブラウザ
- DB 状態（初期化手順）: 不要
- 認証状態（未ログイン/ログイン済み）: どちらでも可

## 4. 変更内容（必要な場合）
- 追加/変更ファイル:
  - `src/AspNetCoreSandbox.Web/Controllers/HomeController.cs`
  - `src/AspNetCoreSandbox.Web/Views/Home/AntiForgeryTokenSource.cshtml`
  - `src/AspNetCoreSandbox.Web/Views/Home/Index.cshtml`
- 変更理由: token 読取優先順位を 1 画面で比較するため。
- ロールバック手順: 追加した action/view/link を元に戻す。

## 5. 実験手順
1. `/Home/AntiForgeryTokenSource` を開く。
2. `POST form token only` を実行して結果を記録する。
3. `POST header token only` を実行して結果を記録する。
4. `POST header valid + form invalid` を実行して結果を記録する。
5. `POST no token` を実行して結果を記録する。

### 5.1 リクエスト例
```http
POST /Home/AntiForgeryTokenSource HTTP/1.1
Host: localhost:7038
Content-Type: application/x-www-form-urlencoded
RequestVerificationToken: <valid token>

scenario=header-valid-form-invalid&__RequestVerificationToken=invalid-token-value
```

### 5.2 期待結果
- 期待するステータスコード: form-only/header-only/header-valid+form-invalid は 200、missing-both は 400
- 期待するレスポンス: 200 ケースでは JSON が返る。
- 期待するログ: 未処理例外が出ないこと。

## 6. 観察結果
- 実際のステータスコード: form-only/header-only/header-valid+form-invalid は 200、missing-both は 400
- 実際のレスポンス:
  - form-only:
    - `hasHeaderToken=false`
    - `hasFormToken=true`
    - `status=200`
  - header-only:
    - `hasHeaderToken=true`
    - `hasFormToken=false`
    - `status=200`
  - header-valid-form-invalid:
    - `hasHeaderToken=true`
    - `hasFormToken=true`
    - `formTokenPreview=invalid-token-value`
    - `status=200`
  - missing-both:
    - 画面上で HTTP 400 を確認
- 実際のログ: 未処理例外なし
- スクリーンショット/ログ保存先:
  - `docs/experiments/artifacts/2026-05-30-antiforgery-token-source-priority/screenshots/localhost_7038_Home_AntiForgeryTokenSource_form-only.png`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-token-source-priority/screenshots/localhost_7038_Home_AntiForgeryTokenSource_header-only.png`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-token-source-priority/screenshots/localhost_7038_Home_AntiForgeryTokenSource_header-valid-form-invalid.png`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-token-source-priority/screenshots/localhost_7038_Home_AntiForgeryTokenSource_missing-both.png`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-token-source-priority/logs/responsebody_form-only.json`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-token-source-priority/logs/responsebody_header-only.json`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-token-source-priority/logs/responsebody_header-valid-form-invalid.json`

## 7. 判定
- 仮説 1 の判定（採択/棄却）: 採択
- 仮説 2 の判定（採択/棄却）: 採択
- 仮説 3 の判定（採択/棄却）: 採択
- 判定理由: form-only/header-only の両方が成功し、header-valid+form-invalid でも成功したため header 優先の仮説と一致した。missing-both は 400 で失敗した。

## 8. 学びと次アクション
- 学び:
  - request token は form でも header でも受理される。
  - header と form の同時送信では、header 側の有効 token が優先される。
  - token 欠落時は 400 で遮断される。
- 未解決事項: 欠落 token と不正 token の失敗メッセージ差分。
- 次にやること: E2（missing vs invalid token）を追加して 400 の理由を分解する。
