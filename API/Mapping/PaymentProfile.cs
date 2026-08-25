using API.DTO.Payment;
using AutoMapper;
using Domain.Models;

namespace API.Mapping
{
    public class PaymentProfile : Profile
    {
        public PaymentProfile()
        {
            CreateMap<CreatePaymentDto, Payment>();

            CreateMap<Payment, PaymentResponseDto>()
                .ForMember(dest => dest.OrderTitle, opt => opt.MapFrom(src => src.Order != null ? src.Order.Title : null));
        }
    }
}