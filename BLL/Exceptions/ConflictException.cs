namespace BLL.Exceptions
{
    /// <summary>The request conflicts with the current state of the entity (HTTP 409).</summary>
    public sealed class ConflictException : AppException
    {
        public ConflictException(string message)
            : base(message, 409)
        {
        }
    }
}
