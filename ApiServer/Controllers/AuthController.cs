using ApiServer.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ApiServer.Controllers
{
    public static class AuthController
    {
        public static IEndpointRouteBuilder UserAuthController(this IEndpointRouteBuilder builder)
        {
            var group = builder.MapGroup("Auth");

            group.MapGet("/register", Register);
            group.MapGet("/login", Login);

            return builder;
        }

        public static async Task<IResult> Register(string login, string password, AuthService authService, JwtTokenGenerator tokenGenerator)
        {
            var account = await authService.AddNewUser(login, AuthService.PasswordHasher(password));
            if (account is null)
            {
                return Results.BadRequest("Couldn't create new user");
            }

            return Results.Ok(tokenGenerator.GenerateJwt(account));
        }

        public static async Task<IResult> Login(string login, string password, AuthService authService, JwtTokenGenerator tokenGenerator)
        {
            var account = await authService.ValidateUser(login, AuthService.PasswordHasher(password));
            if (account is null)
            {
                return Results.BadRequest("Couldn't login user");
            }

            return Results.Ok(tokenGenerator.GenerateJwt(account));
        }
    }
}
