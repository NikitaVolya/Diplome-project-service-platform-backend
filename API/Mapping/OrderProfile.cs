using API.DTO.Order;
using AutoMapper;
using Domain.Models;

namespace API.Mapping
{
    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            CreateMap<Order, OrderResponseDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.UserName : null))
                .ForMember(dest => dest.ExecutorName, opt => opt.MapFrom(src => src.Executor != null ? src.Executor.UserName : null));

            CreateMap<CreateOrderDto, Order>();

            CreateMap<UpdateOrderDto, Order>();
        }
    }
}