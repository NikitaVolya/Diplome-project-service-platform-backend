using AutoMapper;
using Domain.Entities;
using API.DTO.User;

namespace API.Mapping
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<ApplicationUser, UserMeResponseDto>()
                .ForMember(
                    dest => dest.Roles,
                    opt => opt.Ignore());
        }
    }
}
