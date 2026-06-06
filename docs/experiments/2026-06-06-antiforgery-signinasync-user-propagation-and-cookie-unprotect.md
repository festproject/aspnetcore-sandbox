# 実験記録: SignInAsync / SignIn の User 反映タイミングと Cookie Unprotect

## 0. メタ情報
- 実験名: SignInAsync / SignIn の同一リクエスト反映有無 / 次リクエスト反映 / Cookie 復号
- 作成日: 2026-06-06
- 対象: ASP.NET Core Identity Cookie Authentication
- 検証画面: `/Home/AuthCookieIntrospection`

## 1. 目的
1. `HttpContext.SignInAsync` と `Controller.SignIn` 呼び出しで、同一リクエスト内の `HttpContext.User` が変化するか確認する。
2. 次リクエストで `HttpContext.User` が反映されるか確認する。
3. 認証 cookie をサーバー側で `Unprotect` し、claim 一式を復元できるか確認する。

## 2. 実装メモ
- `HomeController` に以下の実験エンドポイントを追加:
  - `POST /Home/AuthCookieIntrospection/sign-in`
  - `POST /Home/AuthCookieIntrospection/sign-in-result`
  - `POST /Home/AuthCookieIntrospection/sign-out`
  - `GET /Home/AuthCookieIntrospection/whoami`
  - `GET /Home/AuthCookieIntrospection/decode-current-cookie`
  - `POST /Home/AuthCookieIntrospection/decode-raw-cookie`
- 復号は `IOptionsMonitor<CookieAuthenticationOptions>` から取得した `TicketDataFormat.Unprotect(cookieValue)` を使用。
- `Program.cs` に `app.UseAuthentication();` を追加。

## 3. 実施手順
1. `Sign out` 実行
2. `SignInAsync(Alice)` 実行
3. 直後にレスポンスの `before` / `after` を確認
4. `SignIn(Alice)` 実行 (`POST /Home/AuthCookieIntrospection/sign-in-result`)
5. 直後にレスポンスの `before` / `after` を確認
6. `WhoAmI (next request)` 実行
7. `Decode current cookie` 実行

## 4. 観察結果
- `SignInAsync(Alice)` 直後レスポンス:
  - `before.userName = (anonymous)`
  - `after.userName = (anonymous)`
  - `authenticateAsyncAfterSignIn.succeeded = false`
  - `responseSetCookieCount = 1`
- `SignIn(Alice)` (`Controller.SignIn(...).ExecuteResultAsync`) 直後レスポンス:
  - `execution = Controller.SignIn(...).ExecuteResultAsync`
  - `before.userName = (anonymous)`
  - `after.userName = (anonymous)`
  - `authenticateAsyncAfterSignIn.succeeded = false`
  - `responseSetCookieCount = 1`
- `WhoAmI (next request)`:
  - `userName = Alice`
  - `isAuthenticated = true`
- `Decode current cookie`:
  - `ticketFound = true`
  - claims:
    - `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name = Alice`
    - `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier = id:Alice`
  - properties:
    - `issuedUtc` と `expiresUtc` が復元される

## 5. 判定
- 同一リクエスト内で `HttpContext.User` は変化しない (`SignInAsync` / `SignIn` ともに): **採択**
- 次リクエストで `HttpContext.User` が反映される: **採択**
- サーバー側で cookie 文字列から claim を復元できる: **採択**

## 6. 補足
- ASP.NET Core で一般的に使うのは `HttpContext.SignInAsync`。`HttpContext.SignIn` は通常利用しない。
- 今回の結果は「`SignInAsync` / `Controller.SignIn` は現在の `User` を直接書き換える API ではなく、主にレスポンス cookie を発行して次リクエストから認証状態を反映する」ことと整合する。
