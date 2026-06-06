using CoreFutsal.Shared.Extensions;
using CoreFutsal.Auth.Services;
using CoreFutsal.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.ValidateRequiredSecrets();
builder.Services.AddFutsalDefaults(builder.Configuration, "Auth Service");
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();
await app.MigrateAsync();
app.UseFutsalDefaults();
app.Run();
