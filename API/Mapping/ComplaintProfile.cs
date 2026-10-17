using API.DTO.Complaint;
using AutoMapper;
using Domain.Models;

namespace API.Mapping
{
    public class ComplaintProfile : Profile
    {
        public ComplaintProfile()
        {
            CreateMap<Complaint, ComplaintResponseDto>()
                .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src => src.Sender != null ? src.Sender.UserName : null))
                .ForMember(dest => dest.TargetUserName, opt => opt.MapFrom(src => src.TargetUser != null ? src.TargetUser.UserName : null));

            CreateMap<CreateComplaintDto, Complaint>();
        }
    }
}