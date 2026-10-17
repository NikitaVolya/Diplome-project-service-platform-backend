namespace BLL.Exceptions
{
    /// <summary>The requested entity does not exist (HTTP 404).</summary>
    public sealed class NotFoundException : AppException
    {
        public NotFoundException(string message)
            : base(message, 404)
        {
        }
    }
}
