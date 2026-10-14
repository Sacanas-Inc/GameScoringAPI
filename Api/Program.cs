using Microsoft.EntityFrameworkCore;
using GameScoringAPI.Extensions;
using GameScoringAPI.Services;
using GameScoringAPI.Services.Validators;
using GameScoringAPI.Mapper;

// builder created using extension method
var builder = WebApplication.CreateBuilder(args);


// Add swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    {
        options.DocumentFilter<OrderTagsDocumentFilter>();
    });

// Configure CORS
builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowSpecificOrigins", builder =>
        {
            builder.WithOrigins("http://localhost:3000", "https://lively-bay-05f413403.5.azurestaticapps.net")
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
    });


// Configure database services (DbContext) and connection.
// This runs before builder.Build(), so no host, ILogger, or console logger provider
// exists yet. If it throws, write the failure to stdout/stderr (which Azure App Service
// captures in the Log Stream at D:\home\LogFiles\stdout) and re-throw so the process
// still exits non-zero instead of failing silently.
try
{
    builder.ConfigureDatabaseServices();
}
catch (Exception ex)
{
    Console.Error.WriteLine("Fatal startup error while configuring the database connection:");
    Console.Error.WriteLine(ex.ToString());
    Console.Out.WriteLine("Fatal startup error while configuring the database connection. See stderr for details.");
    throw;
}

// Register application services
builder.Services.AddScoped<IGameValidator, GameValidator>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IMatchValidator, MatchValidator>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<IMatchDataPointValidator, MatchDataPointValidator>();
builder.Services.AddScoped<IMatchDataPointService, MatchDataPointService>();
builder.Services.AddScoped<MatchMapper>();


var app = builder.Build();

app.Logger.LogInformation("Starting GameScoringAPI in {Environment} environment.", app.Environment.EnvironmentName);

// Database initialization (migrations, triggers)
try
{
    app.InitializeDatabase();
}
catch (Exception ex)
{
    // Reached when Database:FailFastOnStartup is true (or initialization failed before degraded mode engaged).
    app.Logger.LogCritical(ex, "Database initialization failed during startup.");
    throw;
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Game API V0.141");
    c.ConfigObject.AdditionalItems.Add("tagsSorter", "alpha");
    c.RoutePrefix = string.Empty; // Makes Swagger UI available at the app's root
});


app.UseHttpsRedirection();

// Use CORS with the specified policy
app.UseCors("AllowSpecificOrigins");

// Map Game Endpoints
app.MapGameEndpoints();

// Map Match endpoints
app.MapMatchEndpoints();

// Map the DataPoints endpoints
app.MapMatchDataPointEndpoints();

// Test endpoints
app.MapTestEndpoints();

// Health/readiness endpoint (reports database availability)
app.MapHealthEndpoints();

app.Run();

public partial class Program { }