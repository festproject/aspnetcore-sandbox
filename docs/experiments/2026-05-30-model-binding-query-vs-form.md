# 実験計画: Model Binding ラボ

## 0. メタ情報
- 実験名: Query vs Form の優先順位
- 対象機能: ASP.NET Core MVC のモデルバインディング
- 作成日: 2026-05-30
- 担当: Me
- 対象ブランチ: 現在の作業ブランチ

## 1. 背景と目的
- 背景: 同名キーが Query と Form の両方にあるとき、どちらが採用されるかを確認したい。
- 確認したい仕様: Query で開いた初期値は表示されるが、POST では Form の値が優先されるか。
- ゴール（何が分かれば完了か）: 1 回の POST で、Query と Form のどちらが最終値になるか説明できること。

## 2. 仮説
- 仮説 1: GET で `?Name=QueryName` を付けて開くと、初期表示に Query の値が入る。
- 仮説 2: 同じ名前の Form 値を POST すると、最終的に Form の値が採用される。

## 3. 前提条件
- .NET SDK バージョン: 9.0 系
- 実行環境（OS/ブラウザ）: Windows / 任意の Chromium 系ブラウザ
- DB 状態（初期化手順）: 不要
- 認証状態（未ログイン/ログイン済み）: どちらでも可

## 4. 変更内容（必要な場合）
- 追加/変更ファイル:
  - `src/AspNetCoreSandbox.Web/Controllers/HomeController.cs`
  - `src/AspNetCoreSandbox.Web/Models/ModelBindingLabInput.cs`
  - `src/AspNetCoreSandbox.Web/Views/Home/Index.cshtml`
- 変更理由: Query と Form の優先順位を 1 画面で確認できるようにするため。
- ロールバック手順: 追加した action、モデル、ビューを元に戻す。

## 5. 実験手順
1. アプリを起動し、`Home/Index` を開く。
2. `?Name=QueryName` を付けて開き、Name の初期値が Query から入ることを確認する。
3. `?Name=QueryName` が付いた同じ URL のままフォームを POST し、Form の値が最終的に表示されることを確認する。

### 5.1 リクエスト例
```http
GET /Home/Index?Name=QueryName HTTP/1.1
Host: localhost:5001
```

```http
POST /Home/Index?Name=QueryName HTTP/1.1
Host: localhost:5001
Content-Type: application/x-www-form-urlencoded

Name=FormName
```

### 5.2 期待結果
- 期待するステータスコード: 200
- 期待するレスポンス: GET では QueryName が表示され、POST では FormName が最終値として表示される。
- 期待するログ: 未処理例外が出ないこと。

## 6. 観察結果
- 実際のステータスコード: 200
- 実際のレスポンス: GET では QueryName が表示され、POST では FormName が最終値として表示される。
- 実際のログ: 未処理例外なし

## 7. 判定
- 仮説 1 の判定（採択/棄却）: 採択
- 仮説 2 の判定（採択/棄却）: 採択
- 判定理由: GET では QueryName が表示され、同じ URL（query 付き）のまま POST すると FormName が最終表示されたため、想定どおり Query と Form の競合で Form が優先されることを確認できた。

## 8. 学びと次アクション
- 学び: Query で初期表示した値は、POST 時に同名の Form 値があれば上書きされる。競合を正しく確認するには、query を維持した URL に POST する必要がある。
- 未解決事項: Query と Route、Form と Route の競合時の優先順位は未確認。
- 次にやること: 次回は Query vs Route、Form vs Route をそれぞれ 1 実験ずつ分けて確認し、優先順位の表を完成させる。
