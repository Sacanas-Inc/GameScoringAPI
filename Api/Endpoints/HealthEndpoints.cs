using GameScoringAPI.Extensions;
using Microsoft.EntityFrameworkCore;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", async (GameDBContext dbContext, DatabaseInitializationState state) =>
        {
            // Migrations failed (or never ran): report Unhealthy so orchestrators/load balancers can react.
            if (!state.Completed || !state.Succeeded)
            {
                return Results.Json(
                    new { status = "Unhealthy", database = state.Error ?? "Database initialization has not completed." },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            try
            {
                var canConnect = await dbContext.Database.CanConnectAsync();
                if (!canConnect)
                {
                    return Results.Json(
                        new { status = "Unhealthy", database = "Cannot connect to the database." },
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }

                return Results.Ok(new { status = "Healthy" });
            }
            catch (Exception ex)
            {
                return Results.Json(
                    new { status = "Unhealthy", database = ex.Message },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("GetHealth")
        .WithTags("Health", "GET Endpoints")
        .WithOpenApi();
    }
}
