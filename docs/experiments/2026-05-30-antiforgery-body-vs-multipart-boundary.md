# 実験計画: AntiForgery ラボ

## 0. メタ情報
- 実験名: Body(JSON) と multipart/form-data の境界
- 対象機能: ASP.NET Core MVC AntiForgery
- 作成日: 2026-05-30
- 担当: Me
- 対象ブランチ: 現在の作業ブランチ

## 1. 背景と目的
- 背景: request token を header/form のどこから読むかは content-type で挙動が変わる。
- 確認したい仕様:
  1. JSON では header token が必要か。
  2. JSON に form field 名を含めても token として使われないか。
  3. multipart では form token と header token の両方が使えるか。
- ゴール（何が分かれば完了か）: JSON と multipart で token 読取経路の違いを説明できること。

## 2. 仮説
- 仮説 1: JSON + header token は成功する。
- 仮説 2: JSON + body field token のみは失敗する。
- 仮説 3: multipart + form token / multipart + header token は成功し、multipart + no token は失敗する。

## 3. 前提条件
- .NET SDK バージョン: 9.0 系
- 実行環境（OS/ブラウザ）: Windows / Chromium 系ブラウザ
- DB 状態（初期化手順）: 不要
- 認証状態（未ログイン/ログイン済み）: どちらでも可

## 4. 変更内容（必要な場合）
- 追加/変更ファイル:
  - `src/AspNetCoreSandbox.Web/Controllers/HomeController.cs`
  - `src/AspNetCoreSandbox.Web/Views/Home/AntiForgeryBodyMultipart.cshtml`
  - `src/AspNetCoreSandbox.Web/Views/Home/Index.cshtml`
- 変更理由: content-type ごとの token 読取境界を 1 画面で比較するため。
- ロールバック手順: 追加した action/view/link を元に戻す。

## 5. 実験手順
1. `/Home/AntiForgeryBodyMultipart` を開く。
2. `JSON + header token` を実行する。
3. `JSON + body token field only` を実行する。
4. `multipart + form token` を実行する。
5. `multipart + header token only` を実行する。
6. `multipart + no token` を実行する。

### 5.1 リクエスト例
```http
POST /Home/AntiForgeryBodyMultipart/json?scenario=json-header HTTP/1.1
Host: localhost:7038
Content-Type: application/json
RequestVerificationToken: <valid token>

{"age":42}
```

### 5.2 期待結果
- 期待するステータスコード:
  - json-header: 200
  - json-body-field: 400
  - multipart-form: 200
  - multipart-header: 200
  - multipart-missing: 400
- 期待するレスポンス: 200 ケースは JSON を返し、どの token 経路が使えたかを示す。
- 期待するログ: 未処理例外が出ないこと。

## 6. 観察結果
- 実際のステータスコード:
  - json-header: 200
  - json-body-field: 400（スクリーンショットで確認）
  - multipart-form: 200
  - multipart-header: 200
  - multipart-missing: 400（スクリーンショットで確認）
- 実際のレスポンス:
  - json-header: `kind=json`, `boundAge=42`, `hasHeaderToken=true`, `hasFormContentType=false`
  - multipart-form: `kind=multipart`, `boundAge=42`, `hasHeaderToken=false`, `hasFormToken=true`, `uploadedFileName=demo.txt`
  - multipart-header: `kind=multipart`, `boundAge=42`, `hasHeaderToken=true`, `hasFormToken=false`, `uploadedFileName=demo.txt`
  - json-body-field / multipart-missing: 画面上で HTTP 400 を確認
- 実際のログ: 未処理例外なし
- スクリーンショット/ログ保存先:
  - `docs/experiments/artifacts/2026-05-30-antiforgery-body-vs-multipart-boundary/screenshots/localhost_7038_Home_AntiForgeryBodyMultipart_json-header.png`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-body-vs-multipart-boundary/screenshots/localhost_7038_Home_AntiForgeryBodyMultipart_json-body-field.png`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-body-vs-multipart-boundary/screenshots/localhost_7038_Home_AntiForgeryBodyMultipart_multipart-form.png`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-body-vs-multipart-boundary/screenshots/localhost_7038_Home_AntiForgeryBodyMultipart_multipart-header.png`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-body-vs-multipart-boundary/screenshots/localhost_7038_Home_AntiForgeryBodyMultipart_multipart-missing.png`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-body-vs-multipart-boundary/logs/responsebody_json-header.json`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-body-vs-multipart-boundary/logs/responsebody_multipart-form.json`
  - `docs/experiments/artifacts/2026-05-30-antiforgery-body-vs-multipart-boundary/logs/responsebody_multipart-header.json`

## 7. 判定
- 仮説 1 の判定（採択/棄却）: 採択
- 仮説 2 の判定（採択/棄却）: 採択
- 仮説 3 の判定（採択/棄却）: 採択
- 判定理由:
  - JSON は header token ありで成功し、body フィールドへの token 混在では失敗した。
  - multipart は form token と header token のどちらでも成功した。
  - multipart で token 欠落時は 400 で失敗した。

## 8. 学びと次アクション
- 学び:
  - JSON リクエストでは request token は実質 header 経路で扱う必要がある。
  - multipart では form/header の両経路が機能する。
  - content-type によって token 抽出経路が明確に分かれる。
- 未解決事項: multipart でヘッダー優先時に form body 読み取りがどの程度抑制されるか。
- 次にやること: AntiForgery 4本セットの統合サマリーを更新する。
