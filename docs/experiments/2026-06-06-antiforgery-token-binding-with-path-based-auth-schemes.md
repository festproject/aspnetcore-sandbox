# 実験記録: FormTagHelper 自動 token と path-based auth 切替

## 0. メタ情報
- 実験名: FormTagHelper 自動挿入 antiforgery token のユーザー紐付け
- 作成日: 2026-06-06
- 対象: `/Home` と `/admin` で認証スキームが切り替わる構成
- 検証画面:
  - `/Home/AntiForgeryPathAwareAuth`
  - `/admin/antiforgery-path-lab`

## 1. 目的
1. form タグヘルパーで自動挿入される antiforgery token が、GET 時点の current user に紐づくか確認する。
2. POST 先 URL に応じて current auth user が切り替わり、`TryValidateTokenSet` の判定に反映されるか確認する。

## 2. 実装ポイント
- `/Home` 側 current auth: `Identity.Application` (site cookie)
- `/admin` 側 current auth: `AdminCookieScheme` (admin cookie)
- POST 検証エンドポイントは `ValidateRequestAsync` を手動実行し、成功/失敗と current user を JSON で返す。
- 検証フォームは FormTagHelper (`asp-controller` / `asp-action` + `asp-antiforgery="true"`) で token を自動挿入。

## 3. 実施手順
1. site/admin 両スキームにログイン (`SiteAlice`, `AdminBob`)
2. `/Home/AntiForgeryPathAwareAuth` を開き token(H) を取得
3. token(H) を `/Home/.../validate-home` と `/admin/.../validate-admin` に送信
4. `/admin/antiforgery-path-lab` を開き token(A) を取得
5. token(A) を `/admin/.../validate-admin` と `/Home/.../validate-home` に送信

## 4. 観察結果
- token(H) -> `/Home/.../validate-home`
  - `status=200`, `valid=true`, `httpContextUser=SiteAlice`
- token(H) -> `/admin/.../validate-admin`
  - `status=400`, `valid=false`, `httpContextUser=AdminBob`
  - message: `The provided antiforgery token was meant for a different claims-based user than the current user.`
- token(A) -> `/admin/.../validate-admin`
  - `status=200`, `valid=true`, `httpContextUser=AdminBob`
- token(A) -> `/Home/.../validate-home`
  - `status=400`, `valid=false`, `httpContextUser=SiteAlice`
  - message: `The provided antiforgery token was meant for a different claims-based user than the current user.`

## 5. 判定
- 自動挿入 token は GET 時点の current user 文脈に紐づく: **採択**
- POST 時の `TryValidateTokenSet` は、POST 先パスで選択された current user で判定する: **採択**

## 6. 補足
- 同じブラウザセッションに site/admin の 2 cookie が共存していても、`HttpContext.User` は request path によって切り替わる。
- そのため token の再利用可否は「token を発行した user 文脈」と「POST 先で選ばれた user 文脈」の一致で決まる。
