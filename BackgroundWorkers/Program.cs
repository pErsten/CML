using Common.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

var host = builder.Build();

var sqlConnectionStr = builder.Configuration.GetValue<string>("Databases:SqlConnection");
services.AddDbContext<SqlContext>(options => options.UseSqlServer(sqlConnectionStr));


host.Run();

