using AuthServer.Helpers;
using AuthServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Get your encrypted secret from appsettings.json
var encryptedSecret = builder.Configuration["JwtSecretKey"];

// ✅ Register JwtHelper with the secret (Singleton is fine)
builder.Services.AddSingleton<JwtHelper>(sp => new JwtHelper(encryptedSecret));

// ✅ Register your services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthService>();

// ✅ Add MVC + Views + API
builder.Services.AddControllersWithViews();

// ✅ Add HTTP client factory (for AccountController)
builder.Services.AddHttpClient();
// ✅ Add authentication/authorization middleware
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

// Middleware pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();   // for /wwwroot
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ✅ MVC default route (for your Razor UI)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"
);
app.MapControllerRoute(
    name: "login",
    pattern: "Login",
    defaults: new { controller = "Account", action = "Login" }
);
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/InvalidAudience");
}
// ✅ API routes (for /auth/login, etc.)
app.MapControllers();

app.Run();
