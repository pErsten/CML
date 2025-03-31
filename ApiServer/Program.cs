using ApiServer.BackgroundWorkers;
using ApiServer.Controllers;
using Common.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ApiServer.Services;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

var sqlConnectionStr = builder.Configuration.GetValue<string>("Databases:SqlConnection");
services.AddDbContext<SqlContext>(options => options.UseSqlServer(sqlConnectionStr));
services.AddSingleton<JwtTokenGenerator>();
services.AddScoped<AuthService>();

services.AddHostedService<BtcRatesFetcher>();

var jwtKey = Guid.NewGuid().ToString();
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    });
services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();


var authEPs = app.MapGroup("/").RequireAuthorization().WithOpenApi();
var anonEPs = app.MapGroup("/").AllowAnonymous().WithOpenApi();

// Controllers
anonEPs.UserAuthController();

authEPs.UserHomeController();

app.Run();

