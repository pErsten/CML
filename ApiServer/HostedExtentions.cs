using ApiServer.BackgroundWorkers;
using ApiServer.Services;
using Common.Data.Entities;
using Common.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.Channels;
using Common.Data.Models;

namespace ApiServer
{
    public static class HostedExtensions
    {
        /// <summary>
        /// Configures the services required for the web application, including database context, authentication, 
        /// SignalR, CORS policy, and background services.
        /// </summary>
        public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
        {
            var services = builder.Services;

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            
            var eventsChannel = Channel.CreateUnbounded<EventDto>(new UnboundedChannelOptions
            {
                SingleReader = true
            });
            services.AddSingleton(eventsChannel);
            services.AddSingleton(eventsChannel.Writer);
            services.AddSingleton(eventsChannel.Reader);

            var sqlConnectionStr = builder.Configuration.GetValue<string>("Databases:SqlConnection");
            services.AddDbContext<SqlContext>(options => options.UseSqlServer(sqlConnectionStr));

            services.AddScoped<StockMarketService>();
            services.AddScoped<AuthService>();
            services.AddScoped<OrdersService>();
            services.AddSingleton<JwtTokenGenerator>();
            services.AddSingleton<BlazorSignalRService>();
            services.AddHttpContextAccessor();

            services.AddHostedService<BtcRatesFetcher>();
            services.AddHostedService<OrderBookFetcher>();
            services.AddHostedService<EventProceeder>();

            var jwtKey = builder.Configuration.GetValue<string>("Auth:JwtKey");
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateLifetime = true,
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];

                            if (!string.IsNullOrEmpty(accessToken) &&
                                context.HttpContext.Request.Path.StartsWithSegments("/messages"))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });
            services.AddAuthorization();
            services.AddSignalR();
            var webClientUrl = builder.Configuration.GetValue<string>("WebClientUrl");
            services.AddCors(options =>
            {
                options.AddPolicy("WebClient",
                    policy => policy.WithOrigins(webClientUrl)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
            });

            return builder.Build();
        }
    }
}
