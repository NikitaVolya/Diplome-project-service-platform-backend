using API.DTO.Review;
using AutoMapper;
using Domain.Models;

namespace API.Mapping
{
    public class ReviewProfile : Profile
    {
        public ReviewProfile()
        {
            CreateMap<CreateReviewDto, Review>();

            CreateMap<Review, ReviewResponseDto>()
                .ForMember(dest => dest.OrderTitle, opt => opt.MapFrom(src => src.Order != null ? src.Order.Title : null))
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author != null ? src.Author.UserName : null))
                .ForMember(dest => dest.TargetUserName, opt => opt.MapFrom(src => src.TargetUser != null ? src.TargetUser.UserName : null));
        }
    }
}