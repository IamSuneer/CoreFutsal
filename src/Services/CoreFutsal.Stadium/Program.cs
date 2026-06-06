using CoreFutsal.Shared.Extensions;
using CoreFutsal.Shared.Extensions;
using CoreFutsal.Stadium.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.ValidateRequiredSecrets();
builder.Services.AddFutsalDefaults(builder.Configuration, "Stadium Service");
builder.Services.AddScoped<IStadiumService, StadiumService>();

var app = builder.Build();
app.UseFutsalDefaults();
app.Run();
