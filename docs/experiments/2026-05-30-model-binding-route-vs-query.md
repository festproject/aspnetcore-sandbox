# 実験計画: Model Binding ラボ

## 0. メタ情報
- 実験名: Route vs Query の優先順位
- 対象機能: ASP.NET Core MVC のモデルバインディング
- 作成日: 2026-05-30
- 担当: Me
- 対象ブランチ: 現在の作業ブランチ

## 1. 背景と目的
- 背景: 同名キーが Route と Query の両方にあるとき、どちらが採用されるかを確認したい。
- 確認したい仕様: `name` が route と query の両方にある場合、最終的にどちらが action 引数へバインドされるか。
- ゴール（何が分かれば完了か）: 1 回の GET で、Route と Query のどちらが最終値になるか説明できること。

## 2. 仮説
- 仮説 1: route に `RouteName`、query に `QueryName` を与えると、action 引数 `name` は Route の値になる。

## 3. 前提条件
- .NET SDK バージョン: 9.0 系
- 実行環境（OS/ブラウザ）: Windows / 任意の Chromium 系ブラウザ
- DB 状態（初期化手順）: 不要
- 認証状態（未ログイン/ログイン済み）: どちらでも可

## 4. 変更内容（必要な場合）
- 追加/変更ファイル:
  - `src/AspNetCoreSandbox.Web/Controllers/HomeController.cs`
  - `src/AspNetCoreSandbox.Web/Views/Home/RouteVsQuery.cshtml`
- 変更理由: Route と Query の競合を 1 画面で確認できるようにするため。
- ロールバック手順: 追加した action と view を元に戻す。

## 5. 実験手順
1. アプリを起動し、`/Home/RouteVsQuery/RouteName?name=QueryName` を開く。
2. 画面の `Bound name` を確認し、Route と Query のどちらが採用されたかを記録する。

### 5.1 リクエスト例
```http
GET /Home/RouteVsQuery/RouteName?name=QueryName HTTP/1.1
Host: localhost:5001
```

### 5.2 期待結果
- 期待するステータスコード: 200
- 期待するレスポンス: `Bound name` が `RouteName` になる。
- 期待するログ: 未処理例外が出ないこと。

## 6. 観察結果
- 実際のステータスコード: 200
- 実際のレスポンス: `Bound name` が `RouteName` になる。
- 実際のログ: 未処理例外なし

## 7. 判定
- 仮説 1 の判定（採択/棄却）: 採択
- 判定理由: route に RouteName、query に QueryName を与えた同一リクエストで、action 引数へバインドされた値は RouteName だったため、Route と Query の競合では Route が優先されることを確認できた。

## 8. 学びと次アクション
- 学び: 同名キーが Route と Query に同時に存在する場合、MVC のバインディングでは Route の値が採用された。
- 未解決事項: Form と Route の競合時にどちらが優先されるかは未確認。
- 次にやること: 次回は Form vs Route を単独実験として追加し、優先順位表を完成させる。
