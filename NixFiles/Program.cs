using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using NixFiles.Data;
using NixFiles.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024;
});

builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = 50 * 1024 * 1024;
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024;
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IPasswordHasher<Note>, PasswordHasher<Note>>();
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.AccessDeniedPath = "/account/login";
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "tagged",
    pattern: "tags/{tagName:regex(^[A-Za-z0-9-]+$)}",
    defaults: new { controller = "Notes", action = "Tagged" });

app.MapControllerRoute(
    name: "bookmarks",
    pattern: "me/bookmarks",
    defaults: new { controller = "Bookmarks", action = "Index" });

app.MapControllerRoute(
    name: "bookmark-toggle",
    pattern: "bookmarks/{action=Toggle}",
    defaults: new { controller = "Bookmarks" });

app.MapControllerRoute(
    name: "account",
    pattern: "account/{action=Login}",
    defaults: new { controller = "Account" });

app.MapControllerRoute(
    name: "note-unlock",
    pattern: "{name:regex(^[A-Za-z0-9-]+$)}/unlock",
    defaults: new { controller = "Notes", action = "Unlock" });

app.MapControllerRoute(
    name: "note-image",
    pattern: "{name:regex(^[A-Za-z0-9-]+$)}/image",
    defaults: new { controller = "Notes", action = "UploadImage" });

app.MapControllerRoute(
    name: "note-restore",
    pattern: "{name:regex(^[A-Za-z0-9-]+$)}/restore/{versionId:int}",
    defaults: new { controller = "Notes", action = "Restore" });

app.MapControllerRoute(
    name: "note-save",
    pattern: "{name:regex(^[A-Za-z0-9-]+$)}/save",
    defaults: new { controller = "Notes", action = "Save" });

app.MapControllerRoute(
    name: "note",
    pattern: "{name:regex(^[A-Za-z0-9-]+$)}",
    defaults: new { controller = "Notes", action = "Open" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
