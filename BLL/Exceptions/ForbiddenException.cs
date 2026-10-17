namespace BLL.Exceptions
{
    /// <summary>The user is authenticated but not allowed to do this (HTTP 403).</summary>
    public sealed class ForbiddenException : AppException
    {
        public ForbiddenException(string message = "You do not have permission to perform this action.")
            : base(message, 403)
        {
        }
    }
}
