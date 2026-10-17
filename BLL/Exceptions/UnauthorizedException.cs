namespace BLL.Exceptions
{
    /// <summary>The caller could not be authenticated, e.g. a webhook with a bad signature (HTTP 401).</summary>
    public sealed class UnauthorizedException : AppException
    {
        public UnauthorizedException(string message = "Authentication is required.")
            : base(message, 401)
        {
        }
    }
}
