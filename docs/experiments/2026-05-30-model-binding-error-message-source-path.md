# 実験計画: Model Binding ラボ

## 0. メタ情報
- 実験名: 変換値とエラーメッセージ文字列の生成経路
- 対象機能: ASP.NET Core MVC のモデルバインディング
- 作成日: 2026-05-30
- 担当: Me
- 対象ブランチ: 現在の作業ブランチ

## 1. 背景と目的
- 背景: `age=abc&age=42` のような重複値で、なぜエラーメッセージが `abc,42` になるのかを確認したい。
- 確認したい仕様:
  1. 変換対象の値はどこで決まるか。
  2. ModelState のエラーメッセージはどの文字列を使うか。
- ゴール（何が分かれば完了か）: `SimpleTypeModelBinder` が変換用に `FirstValue` を使い、エラー表示には `ValueProviderResult.ToString()` を使うことを説明できること。

## 2. 仮説
- 仮説 1: `SimpleTypeModelBinder` は scalar 変換に `valueProviderResult.FirstValue` を使う。
- 仮説 2: `CheckModel` の null エラーは `valueProviderResult.ToString()` を参照するため、重複値が `abc,42` のように表示される。

## 3. 前提条件
- .NET SDK バージョン: 9.0 系
- 実行環境（OS/ブラウザ）: なし（source inspection）
- DB 状態（初期化手順）: 不要
- 認証状態（未ログイン/ログイン済み）: どちらでも可

## 4. 変更内容（必要な場合）
- 追加/変更ファイル:
  - `src/Mvc/Mvc.Core/src/ModelBinding/Binders/SimpleTypeModelBinder.cs`
  - `src/Mvc/Mvc.Abstractions/src/ModelBinding/ValueProviderResult.cs`
- 変更理由: 実装の参照点を確認し、`abc,42` になる理由を source から説明できるようにするため。
- ロールバック手順: 変更は不要。参照結果を取り消す必要がある場合は本ドキュメントを削除する。

## 5. 実験手順
1. `SimpleTypeModelBinder.BindModelAsync` を読む。
2. `ValueProviderResult.FirstValue` と `ValueProviderResult.ToString()` を読む。
3. `CheckModel` で使われるエラーメッセージ生成式を確認する。

### 5.1 リクエスト例
```http
GET /Home/DuplicateInQuery?age=abc&age=42 HTTP/1.1
Host: localhost:5001
```

### 5.2 期待結果
- 期待するステータスコード: なし（source inspection）
- 期待するレスポンス: なし（source inspection）
- 期待するログ: なし（source inspection）

## 6. 観察結果
- 実際のステータスコード: なし（source inspection）
- 実際のレスポンス: `BindModelAsync` は `valueProviderResult.FirstValue` を変換に使う。`CheckModel` の null エラーは `valueProviderResult.ToString()` を参照する。
- 実際のログ: なし（source inspection）
- スクリーンショット/ログ保存先: なし

## 7. 判定
- 仮説 1 の判定（採択/棄却）: 採択
- 仮説 2 の判定（採択/棄却）: 採択
- 判定理由: scalar 変換は先頭値だけを見る一方、エラーメッセージは複数値をそのまま文字列化するため、`abc,42` のような表示になる。

## 8. 学びと次アクション
- 学び: 重複値の採用値とエラーメッセージの表示元は別で、前者は `FirstValue`、後者は `ToString()`。
- 未解決事項: `StringValues.ToString()` が複数値をカンマ区切りにする実装の追跡。
- 次にやること: 必要なら `StringValues` 側の実装も確認して、`abc,42` の最終生成経路を閉じる。
