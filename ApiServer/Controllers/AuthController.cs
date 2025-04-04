using ApiServer.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ApiServer.Controllers
{
    /// <summary>
    /// Defines authentication endpoints for user registration and login.
    /// Handles user creation, validation, and JWT token generation.
    /// </summary>
    public static class AuthController
    {
        /// <summary>
        /// Registers authentication-related routes for user login and registration.
        /// </summary>
        public static IEndpointRouteBuilder UserAuthController(this IEndpointRouteBuilder builder)
        {
            var group = builder.MapGroup("Auth");

            group.MapGet("/register", Register);
            group.MapGet("/login", Login);

            return builder;
        }

        /// <summary>
        /// Registers a new user with the given login and password, and returns a JWT token if successful.
        /// </summary>
        /// <param name="login">The login name of the new user.</param>
        /// <param name="password">The plaintext password of the new user.</param>
        /// <param name="authService">Service used for user management and validation.</param>
        /// <param name="tokenGenerator">Service used to generate JWT tokens for authenticated users.</param>
        /// <returns>
        /// A 200 OK result with a JWT token if the user is created successfully,
        /// or a 400 Bad Request result if registration fails.
        /// </returns>
        public static async Task<IResult> Register(string login, string password, AuthService authService, JwtTokenGenerator tokenGenerator)
        {
            var account = await authService.AddNewUser(login, AuthService.PasswordHasher(password));
            if (account is null)
            {
                return Results.BadRequest("Couldn't create new user");
            }

            return Results.Ok(tokenGenerator.GenerateJwt(account));
        }

        /// <summary>
        /// Authenticates a user using the provided login and password, returning a JWT token if credentials are valid.
        /// </summary>
        /// <param name="login">The login name of the user.</param>
        /// <param name="password">The plaintext password of the user.</param>
        /// <param name="authService">Service used for user validation.</param>
        /// <param name="tokenGenerator">Service used to generate JWT tokens for authenticated users.</param>
        /// <returns>
        /// A 200 OK result with a JWT token if login is successful,
        /// or a 400 Bad Request result if authentication fails.
        /// </returns>
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
