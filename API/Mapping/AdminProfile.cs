using API.DTO.Admin;
using AutoMapper;
using Domain.Entities;


namespace API.Mapping
{
    public class AdminProfile : Profile
    {
        public AdminProfile()
        {
            CreateMap<ApplicationUser, AdminResponseDto>();
        }
    }

}
