using ApiServer.BackgroundWorkers;
using ApiServer.Controllers;
using Common.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ApiServer.Services;
using Common.Data.Entities;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

var channel = Channel.CreateUnbounded<BitcoinOrder>(new UnboundedChannelOptions
{
    SingleReader = true
});
services.AddSingleton(channel); 
services.AddSingleton(channel.Writer); 
services.AddSingleton(channel.Reader);

var sqlConnectionStr = builder.Configuration.GetValue<string>("Databases:SqlConnection");
services.AddDbContext<SqlContext>(options => options.UseSqlServer(sqlConnectionStr));
services.AddSingleton<JwtTokenGenerator>();
services.AddScoped<AuthService>();
services.AddScoped<WalletService>();
services.AddHttpContextAccessor();

services.AddHostedService<BtcRatesFetcher>();
services.AddHostedService<OrdersManager>();

var jwtKey = builder.Configuration.GetValue<string>("Auth:JwtKey");
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateLifetime = true,
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    });
services.AddAuthorization();
services.AddSignalR();
services.AddCors(options =>
{
    options.AddPolicy("WebClient",
        policy => policy.WithOrigins("https://localhost:7121")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("WebClient");
app.MapHub<SignalRHub>("/messages");
app.UseHttpsRedirection();
app.UseAuthorization();


var authEPs = app.MapGroup("/").RequireAuthorization().WithOpenApi();
var anonEPs = app.MapGroup("/").AllowAnonymous().WithOpenApi();

// Controllers
anonEPs.UserAuthController();

authEPs.UserHomeController();
authEPs.UserOrdersController(app.Environment);

app.Run();

