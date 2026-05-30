# 実験計画: Model Binding ラボ

## 0. メタ情報
- 実験名: 空文字と未送信の境界 + Form同キー重複順序
- 対象機能: ASP.NET Core MVC のモデルバインディング
- 作成日: 2026-05-30
- 担当: Me
- 対象ブランチ: 現在の作業ブランチ

## 1. 背景と目的
- 背景: Formの同名キーについて、キー未送信と空文字送信を区別して確認したい。
- 確認したい仕様:
  1. キー未送信時は Route/Query に進むか。
  2. 空文字送信時は Route/Query に進まず、Form値で確定するか。
  3. 同キー重複時は先頭値で確定し、後続値へフォールバックしないか。
- ゴール: 4パターンのPOST結果を1画面で比較し、上記3点を説明できること。

## 2. 仮説
- 仮説 1: `age` キー未送信なら Form は `None` 扱いになり、Route/Query の値が使われる。
- 仮説 2: `age=` を送信した場合、Formでヒットした扱いとなり、Route/Query へフォールバックしない。
- 仮説 3: `age=&age=42` は先頭の空文字で失敗し、`age=42&age=` は先頭の42で成功する。

## 3. 前提条件
- .NET SDK バージョン: 9.0 系
- 実行環境（OS/ブラウザ）: Windows / 任意の Chromium 系ブラウザ
- DB 状態: 不要
- 認証状態: どちらでも可

## 4. 変更内容
- 追加/変更ファイル:
  - `src/AspNetCoreSandbox.Web/Controllers/HomeController.cs`
  - `src/AspNetCoreSandbox.Web/Views/Home/EmptyVsMissing.cshtml`
  - `src/AspNetCoreSandbox.Web/Views/Home/Index.cshtml`
- 変更理由: 空文字と未送信の差分、および重複順序の影響を最小構成で観測するため。
- ロールバック手順: 上記3ファイルの追加/変更を戻す。

## 5. 実験手順
1. アプリを起動し、`/Home/EmptyVsMissing/123?age=456` を開く。
2. 画面の A, B, C, D の各ボタンを順に押して結果を記録する。

### 5.1 リクエスト例
- A (Missing): フォームに `age` キーを含めずPOST。
- B (Empty only): `age=`
- C (Empty then valid): `age=&age=42`
- D (Valid then empty): `age=42&age=`

### 5.2 期待結果
- A: Bound age は Route または Query 側の値（実装上の優先順位）になる。
- B: Bound age は null になり、ModelState(age) に変換エラーが出る。
- C: Bound age は null になり、ModelState(age) に変換エラーが出る。
- D: Bound age は 42 になる。

## 6. 観察結果
- 実際のステータスコード: すべて200
- 実際のレスポンス:
  - A: Bound age は Route の値になる。
  - B: Bound age は null になり、 **ModelState(age) に変換エラーが表示されない。**
  - C: Bound age は null になり、 **ModelState(age) に変換エラーが表示されない。**
  - D: Bound age は 42 になる。
- 実際のログ: 未処理例外なし

## 7. 判定
- 仮説 1 の判定: 採択
- 仮説 2 の判定: 採択
- 仮説 3 の判定: 採択

## 8. 学びと次アクション
- 学び:
  - キー未送信は `None` になり、値が見つからないときだけ Route/Query へ進む。
  - 空文字は Form で値ありとして扱われるが、`int?` のような nullable simple type では `null` 扱いで成功し、ModelState エラーにならない。
  - `age=abc` は文字列からの変換失敗なので ModelState エラーになるが、空文字はその経路に入らない。
- 未解決事項: 失敗時メッセージ文字列の生成経路。
- 次にやること: この結果を統合サマリーの「未送信 vs 空文字」境界として短く追記する。
