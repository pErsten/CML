using ApiServer.Controllers;
using ApiServer;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Information)
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.ConfigureServices();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("WebClient");
app.MapHub<BlazorSignalRHub>("/messages");
app.UseHttpsRedirection();
app.UseAuthorization();


var authEPs = app.MapGroup("/").RequireAuthorization().WithOpenApi();
var anonEPs = app.MapGroup("/").AllowAnonymous().WithOpenApi();

// Controllers
anonEPs.UserAuthController();

app.Run();

