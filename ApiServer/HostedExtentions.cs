using ApiServer.BackgroundWorkers;
using ApiServer.Services;
using Common.Data.Entities;
using Common.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.Channels;

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
            services.AddSingleton<BlazorSignalRService>();
            services.AddHttpContextAccessor();

            services.AddHostedService<BtcRatesFetcher>();
            services.AddHostedService<OrdersManager>();

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
                        },
                        OnTokenValidated = context =>
                        {
                            Console.WriteLine($"[DEBUG] Token validated for: {context.Principal.Identity.Name}");
                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine($"[ERROR] Token authentication failed: {context.Exception.Message}");
                            return Task.CompletedTask;
                        }
                    };
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

            return builder.Build();
        }
    }
}
