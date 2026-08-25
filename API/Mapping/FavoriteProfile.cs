using API.DTO.Favorite;
using AutoMapper;
using Domain.Models;

namespace API.Mapping
{
    public class FavoriteProfile : Profile
    {
        public FavoriteProfile()
        {
            CreateMap<Favorite, FavoriteOrderResponseDto>()
                .ForMember(dest => dest.OrderTitle, opt => opt.MapFrom(src => src.TargetOrder != null ? src.TargetOrder.Title : null))
                .ForMember(dest => dest.OrderPrice, opt => opt.MapFrom(src => src.TargetOrder != null ? src.TargetOrder.Price : (decimal?)null))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.TargetOrder != null && src.TargetOrder.Category != null ? src.TargetOrder.Category.Name : null));

            CreateMap<Favorite, FavoriteExecutorResponseDto>()
                .ForMember(dest => dest.ExecutorUserName, opt => opt.MapFrom(src => src.TargetExecutor != null ? src.TargetExecutor.UserName : null))
                .ForMember(dest => dest.ExecutorFullName, opt => opt.MapFrom(src => src.TargetExecutor != null ? $"{src.TargetExecutor.FirstName} {src.TargetExecutor.LastName}".Trim() : null));
                //.ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.TargetExecutor != null ? src.TargetExecutor.Rating : (double?)null));
        }
    }
}