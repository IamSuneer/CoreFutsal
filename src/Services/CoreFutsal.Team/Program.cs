using CoreFutsal.Shared.Extensions;
using CoreFutsal.Shared.Extensions;
using CoreFutsal.Team.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.ValidateRequiredSecrets();
builder.Services.AddFutsalDefaults(builder.Configuration, "Team Service");
builder.Services.AddScoped<ITeamService, TeamService>();

var app = builder.Build();
app.UseFutsalDefaults();
app.Run();
