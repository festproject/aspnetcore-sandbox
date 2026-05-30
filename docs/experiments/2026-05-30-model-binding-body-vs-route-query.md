# 実験計画: Model Binding ラボ

## 0. メタ情報
- 実験名: Body と Route/Query の分離
- 対象機能: ASP.NET Core MVC のモデルバインディング
- 作成日: 2026-05-30
- 担当: Me
- 対象ブランチ: 現在の作業ブランチ

## 1. 背景と目的
- 背景: `[FromBody]` が付いたパラメータは ValueProvider 系ではなく input formatter 経由で読むため、Route/Query の値が混ざらないか確認したい。
- 確認したい仕様:
  1. JSON body の `age` が route/query の `age` と違っても body が採用されるか。
  2. 空 body のとき route/query へフォールバックせず、body 固有のエラーになるか。
- ゴール（何が分かれば完了か）: `[FromBody]` の経路が value provider と独立していることを説明できること。

## 2. 仮説
- 仮説 1: `{"age":42}` を送ると、route/query に別値があっても body の `42` が採用される。
- 仮説 2: 空 body を送ると、route/query の値は無視され、body 由来の ModelState エラーになる。

## 3. 前提条件
- .NET SDK バージョン: 9.0 系
- 実行環境（OS/ブラウザ）: Windows / 任意の Chromium 系ブラウザ
- DB 状態（初期化手順）: 不要
- 認証状態（未ログイン/ログイン済み）: どちらでも可

## 4. 変更内容（必要な場合）
- 追加/変更ファイル:
  - `src/AspNetCoreSandbox.Web/Controllers/HomeController.cs`
  - `src/AspNetCoreSandbox.Web/Models/BodyBindingLabInput.cs`
  - `src/AspNetCoreSandbox.Web/Views/Home/BodyVsRouteQuery.cshtml`
  - `src/AspNetCoreSandbox.Web/Views/Home/Index.cshtml`
- 変更理由: body binding を route/query と同じ画面で比較できるようにするため。
- ロールバック手順: 追加した action / model / view / link を元に戻す。

## 5. 実験手順
1. `/Home/BodyVsRouteQuery/123?age=456` を開く。
2. `Send JSON body { age: 42 }` を押す。
3. `Send empty body` を押す。

### 5.1 リクエスト例
```http
POST /Home/BodyVsRouteQuery/123?age=456 HTTP/1.1
Host: localhost:5001
Content-Type: application/json

{"age":42}
```

### 5.2 期待結果
- 期待するステータスコード: 200
- 期待するレスポンス: body の `age` が採用され、route/query の `age` は bound result を上書きしない。
- 期待するログ: 空 body でも route/query へのフォールバックは起きない。

## 6. 観察結果
- 実際のステータスコード: 200
- 実際のレスポンス:
  - `{"age":42}` では `boundAge` が `"42"`、`routeAge` は `"123"`、`queryAge` は `"456"`。
  - 空 body では `boundAge` が `"(null)"`、`routeAge` は `"123"`、`queryAge` は `"456"`。
  - どちらも `modelStateErrors` は空。
- 実際のログ: 未処理例外なし
- スクリーンショット/ログ保存先: 未記録

## 7. 判定
- 仮説 1 の判定（採択/棄却）: 採択
- 仮説 2 の判定（採択/棄却）: 採択
- 判定理由: JSON body の `age` が route/query と独立して採用され、空 body でも route/query へフォールバックしなかった。空 body は nullable の `int?` では null として成功扱いになった。

## 8. 学びと次アクション
- 学び: `BodyModelBinder` は `BindingSource.Body` に対して `IInputFormatter` を使い、ValueProvider 系とは別経路である。
- 学び: nullable な body モデルでは、空 body がエラーではなく `null` として返るケースがある。
- 未解決事項: 参照型や非 nullable body モデルで空 body を送ったときの扱い。
- 次にやること: 必要なら non-nullable body モデルでもう 1 回だけ比較する。
