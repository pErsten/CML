namespace ApiServer.Utils
{
    /// <summary>
    /// Provides utility methods for web-related operations.
    /// </summary>
    public static class WebUtils
    {
        /// <summary>
        /// Retrieves the user ID from the current HTTP context's user identity.
        /// </summary>
        /// <param name="context">The HTTP context containing the user's identity.</param>
        /// <returns>The user ID if available, otherwise <c>null</c>.</returns>
        public static string? UserId(this HttpContext context)
            => context.User.Identity?.Name;
    }
}
