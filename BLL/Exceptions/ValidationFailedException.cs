namespace BLL.Exceptions
{
    /// <summary>The request data is invalid (HTTP 400).</summary>
    public sealed class ValidationFailedException : AppException
    {
        public ValidationFailedException(string message)
            : base(message, 400)
        {
        }
    }
}
