using NetworkDrive.Application.UseCases.BrowseFolder;
using NetworkDrive.Domain.Interfaces;
using NetworkDrive.Infrastructure.Storage;
using NetworkDrive.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.Configure<StorageOptions>(
    builder.Configuration.GetSection("Storage:FileSystem"));

builder.Services.AddScoped<IStorageRepository, LocalStorageRepository>();
builder.Services.AddSingleton<INetworkShareAuthService, NetworkShareAuthService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<INetworkCredentialProvider, HttpContextNetworkCredentialProvider>();
builder.Services.AddScoped<INetworkImpersonator, NetworkImpersonator>();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<BrowseFolderQuery>());

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
