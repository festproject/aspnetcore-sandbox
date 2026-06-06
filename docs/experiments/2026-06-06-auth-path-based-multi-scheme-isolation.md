# 実験記録: Path-based multi-scheme isolation (`/admin` とサイトトップの分離)

## 0. メタ情報
- 実験名: サイト用ログインと管理画面ログインの分離
- 作成日: 2026-06-06
- 対象: ASP.NET Core Cookie Authentication (`/admin` パスでスキーム切替)
- 検証画面:
  - `/Home/PathBasedSchemeIsolation`
  - `/admin/scheme-lab`

## 1. 目的
1. サイトトップと管理画面のログイン状態を Cookie スキームで分離する。
2. `HttpContext.User` がリクエストパスに応じて別ユーザーを返すことを確認する。

## 2. 実装メモ
- 既存のサイト側は `IdentityConstants.ApplicationScheme` を利用。
- 管理画面向けに `AdminCookieScheme` を追加。
- デフォルト認証は `PathAwareCookieScheme` (PolicyScheme) を使用。
- `ForwardDefaultSelector` で次のように分岐:
  - `^/admin($|/)` に一致するパス: `AdminCookieScheme`
  - それ以外: `IdentityConstants.ApplicationScheme`

## 3. 検証手順
1. 両 Cookie を sign-out
2. サイト用で sign-in (`SiteAlice`)
3. 管理用で sign-in (`AdminBob`)
4. `/Home/PathBasedSchemeIsolation/whoami` を呼ぶ
5. `/admin/scheme-lab/whoami` を呼ぶ

## 4. 観察結果
- `GET /Home/PathBasedSchemeIsolation/whoami`
  - `httpContextUser.userName = SiteAlice`
  - `currentDefaultAuthenticate.userName = SiteAlice`
  - `siteSchemeAuthenticate.userName = SiteAlice`
  - `adminSchemeAuthenticate.userName = AdminBob`
- `GET /admin/scheme-lab/whoami`
  - `httpContextUser.userName = AdminBob`
  - `currentDefaultAuthenticate.userName = AdminBob`
  - `siteSchemeAuthenticate.userName = SiteAlice`
  - `adminSchemeAuthenticate.userName = AdminBob`

## 5. 判定
- `/Home` と `/admin` で `HttpContext.User` を別ユーザーとして運用できる: **採択**
- 同じブラウザセッションで 2 つの Cookie スキームを同時保持できる: **採択**
- 同一リクエスト内で `HttpContext.User` は 1 つだが、選択スキームはパスで切り替わる: **採択**

## 6. 補足
- Cookie を分離しても、明示的に `AuthenticateAsync("scheme")` を呼べば各スキームの Principal を個別に取得できる。
- そのため実験 API は `HttpContext.User` と各スキームの `AuthenticateAsync` 結果を併記している。
