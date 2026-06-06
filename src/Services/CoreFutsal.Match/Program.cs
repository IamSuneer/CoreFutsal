using CoreFutsal.Shared.Extensions;
using CoreFutsal.Match.Services;
using CoreFutsal.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.ValidateRequiredSecrets();
builder.Services.AddFutsalDefaults(builder.Configuration, "Match Service");
builder.Services.AddScoped<IMatchService, MatchService>();

var app = builder.Build();
app.UseFutsalDefaults();
app.Run();
