# 実験計画: AntiForgery ラボ

## 0. メタ情報
- 実験名: SignInAsync 後の旧トークン失効と Username 大小比較
- 対象機能: ASP.NET Core MVC AntiForgery
- 作成日: 2026-06-03
- 担当: Me
- 対象ブランチ: 現在の作業ブランチ

## 1. 背景と目的
- 背景: `HttpContext.SignInAsync` 後に過去レスポンスの token で `TryValidateTokenSet` が失敗する現象を再現したい。
- 確認したい仕様:
  1. 匿名時点で発行した request token が、サインイン後に失効するか。
  2. `Username` の比較が `OrdinalIgnoreCase` で、`Alice` と `alice` が一致するか。
- ゴール（何が分かれば完了か）: SignIn 前後と名前の大小違いで、token が失敗/成功する条件を説明できること。

## 2. 仮説
- 仮説 1: 匿名時に mint した token は、サインイン後の protected POST で 400 になる。
- 仮説 2: `Alice` で mint した token は、`alice` で再サインインしても protected POST が通る。

## 3. 前提条件
- .NET SDK バージョン: 10.0 系
- 実行環境（OS/ブラウザ）: Windows / Chromium 系ブラウザ
- DB 状態（初期化手順）: 不要
- 認証状態（未ログイン/ログイン済み）: どちらでも可

## 4. 変更内容（必要な場合）
- 追加/変更ファイル:
  - `src/AspNetCoreSandbox.Web/Controllers/HomeController.cs`
  - `src/AspNetCoreSandbox.Web/Views/Home/AntiForgerySignInInvalidation.cshtml`
  - `src/AspNetCoreSandbox.Web/Views/Home/Index.cshtml`
  - `src/AspNetCoreSandbox.Web/Areas/Identity/Pages/Account/Login.cshtml*`
  - `src/AspNetCoreSandbox.Web/Areas/Identity/Pages/Account/Register.cshtml*`
- 変更理由: SignIn 前後と Username 比較の条件を 1 画面で再現するため。
- ロールバック手順: 追加した action/view/link を元に戻す。

## 5. 実験手順
1. `/Home/AntiForgerySignInInvalidation` を開く。
2. `Sign out` を押して匿名状態にする。
3. `Mint token snapshot` を押す（匿名 token を保持）。
4. `SignInAsync username=Alice` を押す。
5. `POST protected with snapshot token + age=abc` を押す（失敗確認）。
6. `Mint token snapshot` を押し直す（Alice token を保持）。
7. `SignInAsync username=alice` を押す。
8. `POST protected with snapshot token + age=abc` を押す（成功確認）。

### 5.1 リクエスト例
```http
POST /Home/AntiForgerySignInInvalidation/protected HTTP/1.1
Host: localhost:7038
Content-Type: application/x-www-form-urlencoded

scenario=post-snapshot&age=abc&__RequestVerificationToken=<snapshot-token>
```

### 5.2 期待結果
- 期待するステータスコード:
  - 匿名 token + サインイン後 protected: 400
  - Alice token + alice サインイン後 protected: 200
- 期待するレスポンス: 200 ケースは JSON で `X-Action-Reached=true` と `ageErrors` が返る。
- 期待するログ: 未処理例外なし。

## 6. 観察結果
- 実際のステータスコード:
  - 匿名 token + `SignInAsync("Alice")` 後 protected: 400
  - `Alice` token + `SignInAsync("alice")` 後 protected: 400
  - 対照実験 `Alice` token + `SignInAsync("Alice")` 後 protected: 200
- 実際のレスポンス:
  - 400 ケース: `HTTP 400` かつ `X-Action-Reached: false`（action 未到達）
  - 200 ケース: `HTTP 200` かつ `X-Action-Reached: true`、JSON で `currentUserName:"Alice"` と `ageErrors:["The value 'abc' is not valid."]`
- 実際のログ:
  - サーバーは Development で起動し、未処理例外は発生しなかった。
- スクリーンショット/ログ保存先:
  - Playwright snapshot: `.playwright-mcp/page-2026-06-03T14-18-07-244Z.yml`
  - Playwright console log: `.playwright-mcp/console-2026-06-03T14-18-07-086Z.log`

## 7. 判定
- 仮説 1 の判定（採択/棄却）: 採択
- 仮説 2 の判定（採択/棄却）: 棄却
- 判定理由:
  - 仮説 1: 匿名で mint した token は `SignInAsync` 後に 400 で失効した。
  - 仮説 2: `Alice` で mint 後に `alice` へサインインすると 400 になり、期待した case-insensitive 成功は再現しなかった。
  - 追加対照で `Alice -> Alice` は 200 のため、単なる再サインインではなく identity 情報差分（case 差）に依存して失敗している。

## 8. 学びと次アクション
- 学び:
  - AntiForgery は action 到達前に 400 を返し得るため、`X-Action-Reached` で short-circuit を明確に観測できる。
  - 本実装では `Alice` と `alice` を同一扱いにしない経路が存在し、`Username` 単純比較だけでは説明できない可能性がある。
- 未解決事項:
  - `ClaimUidExtractor` 側の計算結果が case 差で変化しているか（`Username` ではなく `ClaimUid` 比較に入っているか）の source-level 確認。
  - URI 形式 username (`https://...`) の case-sensitive 分岐との関係。
- 次にやること:
  - `DefaultAntiforgeryTokenGenerator.TryValidateTokenSet` と `ClaimUidExtractor` を追って、今回の 400 の直接原因を統合サマリーに反映する。
