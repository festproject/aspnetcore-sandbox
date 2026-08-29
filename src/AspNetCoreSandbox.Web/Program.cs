using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AspNetCoreSandbox.Web.Data;
using PageState;

var builder = WebApplication.CreateBuilder(args);
const string AdminCookieScheme = "AdminCookieScheme";
const string PathAwareCookieScheme = "PathAwareCookieScheme";

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = PathAwareCookieScheme;
        options.DefaultChallengeScheme = PathAwareCookieScheme;
    })
    .AddPolicyScheme(PathAwareCookieScheme, PathAwareCookieScheme, options =>
    {
        options.ForwardDefaultSelector = context =>
            context.Request.Path.StartsWithSegments("/admin") ? AdminCookieScheme : IdentityConstants.ApplicationScheme;
    })
    .AddCookie(AdminCookieScheme, options =>
    {
        options.Cookie.Name = "AspNetCoreSandbox.AdminAuth";
        options.LoginPath = "/admin/scheme-lab";
        options.AccessDeniedPath = "/admin/scheme-lab";
    });

builder.Services.AddControllersWithViews();

builder.Services.AddDataProtection()
    .SetApplicationName("AspNetCoreSandbox");
builder.Services.AddPageState();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();

// Exposed so WebApplicationFactory<Program> in PageState.IntegrationTests can host this app.
public partial class Program { }
