# ASP.NET Core Sandbox

新規プロジェクトの作成

```
dotnet new sln -n AspNetCoreSandbox -o aspnetcore-sandbox
cd aspnetcore-sandbox
dotnet new mvc -n AspNetCoreSandbox.Web -o src/AspNetCoreSandbox.Web --auth Individual
dotnet sln add src/AspNetCoreSandbox.Web/AspNetCoreSandbox.Web.csproj
```

.gitignoreの作成

```
dotnet new gitignore
```

## 実験計画

実験計画の詳細は `docs/experiments` 配下に置きます。

- テンプレート: `docs/experiments/EXPERIMENT_PLAN_TEMPLATE.md`
- 運用ルール: 1 実験につき 1 ファイルを作成し、手順・期待結果・観察結果を記録する
- 統合サマリー: `docs/experiments/2026-05-30-model-binding-findings-summary.md`
