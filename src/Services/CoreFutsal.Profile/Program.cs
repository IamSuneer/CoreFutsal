using CoreFutsal.Shared.Extensions;
using CoreFutsal.Profile.Services;
using CoreFutsal.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.ValidateRequiredSecrets();
builder.Services.AddFutsalDefaults(builder.Configuration, "Profile Service");
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<IMarketplaceService, MarketplaceService>();

var app = builder.Build();
app.UseFutsalDefaults();
app.Run();
