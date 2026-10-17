namespace BLL.Exceptions
{
    /// <summary>
    /// Base class for errors that are expected parts of the business flow.
    /// The message is safe to show to the API client; StatusCode is the HTTP status
    /// the GlobalExceptionHandler will answer with.
    /// </summary>
    public abstract class AppException : Exception
    {
        protected AppException(string message, int statusCode, Exception? innerException = null)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        public int StatusCode { get; }
    }
}
