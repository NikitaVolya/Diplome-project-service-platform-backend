using API.DTO.Category;
using AutoMapper;
using Domain.Models;

namespace API.Mapping
{
    public class CategoryProfile : Profile
    {
        public CategoryProfile()
        {
            CreateMap<Category, CategoryResponseDto>()
                .ForMember(dest => dest.ParentCategoryName, opt => opt.MapFrom(src => src.ParentCategory != null ? src.ParentCategory.Name : null))
                .ForMember(dest => dest.SubCategories, opt => opt.MapFrom(src => src.SubCategories));

            CreateMap<CreateCategoryDto, Category>();

            CreateMap<UpdateCategoryDto, Category>();
        }
    }
}