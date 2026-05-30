# 実験計画: Model Binding ラボ

## 0. メタ情報
- 実験名: Form vs Route の優先順位
- 対象機能: ASP.NET Core MVC のモデルバインディング
- 作成日: 2026-05-30
- 担当: Me
- 対象ブランチ: 現在の作業ブランチ

## 1. 背景と目的
- 背景: 同名キーが Form と Route の両方にあるとき、どちらが採用されるかを確認したい。
- 確認したい仕様: `name` が route と form の両方にある場合、最終的にどちらが action 引数へバインドされるか。
- ゴール（何が分かれば完了か）: 1 回の POST で、Form と Route のどちらが最終値になるか説明できること。

## 2. 仮説
- 仮説 1: route に `RouteName`、form に `FormName` を与えると、action 引数 `name` は Form の値になる。

## 3. 前提条件
- .NET SDK バージョン: 9.0 系
- 実行環境（OS/ブラウザ）: Windows / 任意の Chromium 系ブラウザ
- DB 状態（初期化手順）: 不要
- 認証状態（未ログイン/ログイン済み）: どちらでも可

## 4. 変更内容（必要な場合）
- 追加/変更ファイル:
  - `src/AspNetCoreSandbox.Web/Controllers/HomeController.cs`
  - `src/AspNetCoreSandbox.Web/Views/Home/FormVsRoute.cshtml`
- 変更理由: Form と Route の競合を 1 画面で確認できるようにするため。
- ロールバック手順: 追加した action と view を元に戻す。

## 5. 実験手順
1. アプリを起動し、`/Home/FormVsRoute/RouteName` を開く。
2. フォームの `name` 入力欄に `FormName` を入れて送信する。
3. 画面の `Bound name` を確認し、Form と Route のどちらが採用されたかを記録する。

### 5.1 リクエスト例
```http
POST /Home/FormVsRoute/RouteName HTTP/1.1
Host: localhost:5001
Content-Type: application/x-www-form-urlencoded

name=FormName
```

### 5.2 期待結果
- 期待するステータスコード: 200
- 期待するレスポンス: `Bound name` が `FormName` になる。
- 期待するログ: 未処理例外が出ないこと。

## 6. 観察結果
- 実際のステータスコード: 200
- 実際のレスポンス: `Bound name` が `FormName` になる。
- 実際のログ: 未処理例外なし

## 7. 判定
- 仮説 1 の判定（採択/棄却）: 採択
- 判定理由: route に RouteName、form に FormName を与えた POST で、action 引数 name の最終値は FormName になったため、Form と Route の競合では Form が優先されることを確認できた。

## 8. 学びと次アクション
- 学び: 同名キー競合で Form が Route より優先されるのは、MVC の既定 ValueProvider 登録順と取得処理の実装と一致している。
- 学び（ソース根拠 1）: `aspnetcore/src/Mvc/Mvc.Core/src/Infrastructure/MvcCoreMvcOptionsSetup.cs` で ValueProvider は `FormValueProviderFactory` -> `RouteValueProviderFactory` -> `QueryStringValueProviderFactory` の順で登録されている。
- 学び（ソース根拠 2）: `aspnetcore/src/Mvc/Mvc.Core/src/ModelBinding/Binders/SimpleTypeModelBinder.cs` で `bindingContext.ValueProvider.GetValue(bindingContext.ModelName)` を呼び出して値を取得している。
- 学び（ソース根拠 3）: `aspnetcore/src/Mvc/Mvc.Core/src/ModelBinding/CompositeValueProvider.cs` の `GetValue` は内部 `Items` を先頭から走査し、最初に見つかった `ValueProviderResult` を返している。
- 未解決事項: なし（今回の Form vs Route の結論は、実測と実装根拠の両方で確認済み）。
- 次にやること: 必要になった時点で、同じ形式で個別ケース（例: Query vs Form、Route vs Query）を追加検証する。
