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