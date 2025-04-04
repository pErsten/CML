using ApiServer.Controllers;
using ApiServer;

var builder = WebApplication.CreateBuilder(args);
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

authEPs.UserHomeController();
authEPs.UserOrdersController(app.Environment);

app.Run();

