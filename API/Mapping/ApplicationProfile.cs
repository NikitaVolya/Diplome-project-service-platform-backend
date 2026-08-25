using API.DTO.Application;
using AutoMapper;
using Domain.Models;

namespace API.Mapping
{
    public class ApplicationProfile : Profile
    {
        public ApplicationProfile()
        {
            CreateMap<Application, ApplicationResponseDto>()
                .ForMember(dest => dest.OrderTitle, opt => opt.MapFrom(src => src.Order != null ? src.Order.Title : null))
                .ForMember(dest => dest.ExecutorName, opt => opt.MapFrom(src => src.Executor != null ? src.Executor.UserName : null));
        }
    }
}