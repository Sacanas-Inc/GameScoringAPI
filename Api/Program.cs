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
builder.ConfigureDatabaseServices();

// Register application services
builder.Services.AddScoped<IGameValidator, GameValidator>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IMatchValidator, MatchValidator>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<IMatchDataPointValidator, MatchDataPointValidator>();
builder.Services.AddScoped<IMatchDataPointService, MatchDataPointService>();
builder.Services.AddScoped<MatchMapper>();


var app = builder.Build();

// Database initialization (migrations, triggers)
app.InitializeDatabase();

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

app.Run();

public partial class Program { }