var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/slow", async () =>
{
    await Task.Delay(TimeSpan.FromSeconds(5));
    return "slow";
});

app.MapGet("/transientError", async context =>
{
    await Task.Delay(TimeSpan.FromMilliseconds(500));
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
});

app.MapGet("/timeout", async context =>
{
    await Task.Delay(TimeSpan.FromMilliseconds(500));
    context.Response.StatusCode = 408;
});

app.Run();