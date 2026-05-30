# Model Binding Findings Summary (2026-05-30)

このドキュメントは、当日の Model Binding 実験結果を 1 ページに統合したものです。

## 1. 優先順位（同名キー競合）

結論: Form > Route > Query

根拠（実験）
- Query vs Form: Form が採用された。
- Route vs Query: Route が採用された。
- Form vs Route: Form が採用された。

根拠（ASP.NET Core 実装）
- `MvcCoreMvcOptionsSetup` の既定 `ValueProviderFactories` 登録順が Form -> Route -> Query。
  - `FormValueProviderFactory`
  - `RouteValueProviderFactory`
  - `QueryStringValueProviderFactory`
- `CompositeValueProvider.GetValue` は先頭から探索し、最初に見つかった値を返す。

## 2. フォールバック有無

結論: 変換失敗しても次ソースへフォールバックしない。

根拠
- `age=abc`（Form）と `age=123`（Route）`age=456`（Query）を同時に与えても、
  `Bound age` は null、`ModelState(age)` は変換エラーとなった。
- `SimpleTypeModelBinder` は `bindingContext.ValueProvider.GetValue(...)` で取得した値を変換し、
  失敗時は ModelState エラーを追加する。次ソースへの再探索処理は行わない。

## 3. 同一ソース内で同名キーが複数ある場合（scalar）

結論: 先頭値で結果が決まる。先頭失敗時に後続値へフォールバックしない。

観察
- Query: `?age=abc&age=42` は失敗、`?age=42&age=abc` は成功。
- Form: `age=abc&age=42` は失敗、`age=42&age=abc` は成功。
- 失敗時メッセージは `The value 'abc,42' is not valid.` となるケースを確認。

## 4. 失敗の定義（未送信 vs 検証失敗）

結論
- `[BindRequired]`: キー未送信をバインド段階でエラー化。
- `[Required]`: null/空値を検証段階でエラー化。

## 5. 空文字と未送信の境界

結論: キー未送信と空文字送信は別扱い。

根拠
- `age` キー未送信では、Bound age は Route の値になった。
- `age=` と `age=&age=42` は Bound age が null になったが、nullable の `int?` では ModelState(age) に変換エラーは出なかった。
- `age=abc` のような非数値入力とは違い、空文字は型変換失敗に入らない。

## 6. エラーメッセージの生成経路

結論: 変換には `FirstValue`、エラーメッセージには `ToString()` を使う。

根拠
- `SimpleTypeModelBinder` は scalar の変換で `valueProviderResult.FirstValue` を使う。
- `CheckModel` の null エラーは `valueProviderResult.ToString()` を参照する。
- そのため `age=abc&age=42` のエラーメッセージは `abc,42` になる。

## 7. Body と ValueProvider の分離

結論: `[FromBody]` は input formatter 経由で読み、Form/Route/Query の ValueProvider とは別経路。

根拠
- `BodyModelBinder` は `BindingSource.Body` に対して `IInputFormatter` を選択する。
- `BodyModelBinderProvider` は `BindingSource.Body` を受けると body binder を返す。
- そのため body の `age` は route/query の `age` と競合しても、同じ採用ルールにはならない。
- 実験では `{"age":42}` を送ると body の `42` が採用され、route/query の `123` / `456` は上書きしなかった。
- 空 body では route/query にフォールバックせず、nullable の body モデルは `null` で成功した。

## 8. 実験一覧

- `2026-05-30-model-binding-query-vs-form.md`
- `2026-05-30-model-binding-route-vs-query.md`
- `2026-05-30-model-binding-form-vs-route.md`
- `2026-05-30-model-binding-no-fallback-on-bind-failure.md`
- `2026-05-30-model-binding-duplicate-keys-in-query.md`
- `2026-05-30-model-binding-duplicate-keys-in-form.md`
- `2026-05-30-model-binding-failure-definitions-required-vs-bindrequired.md`
- `2026-05-30-model-binding-empty-vs-missing-and-form-duplicate-order.md`

- `2026-05-30-model-binding-error-message-source-path.md`
- `2026-05-30-model-binding-body-vs-route-query.md`

## 9. 今後やるなら

- Body（FromBody/JSON）を含めたときの適用範囲と、ValueProvider ベースの挙動との差を整理する。
