using API.DTO.Authentication;
using AutoMapper;
using BLL.Results;
using Domain.Entities;

namespace API.Mapping
{
    public class AuthenticationProfile : Profile
    {
        public AuthenticationProfile()
        {
            CreateMap<ApplicationUser, UserResponseDto>()
                .ForMember(d => d.Username,
                    o => o.MapFrom(s => s.UserName));

            CreateMap<AuthenticationResult, LoginResponseDto>();

            CreateMap<ApplicationUser, UserResponseDto>();
        }
    }
}
