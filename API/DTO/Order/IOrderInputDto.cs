namespace API.DTO.Order
{
    public interface IOrderInputDto
    {
        string Title { get; }
        string Description { get; }
        decimal Price { get; }
        int CategoryId { get; }
        string Address { get; }
        double Latitude { get; }
        double Longitude { get; }
    }
}
