using API.DTO.OrderMessage;
using AutoMapper;
using Domain.Models;

namespace API.Mapping
{
    public class OrderMessageProfile : Profile
    {
        public OrderMessageProfile()
        {
            CreateMap<OrderMessage, OrderMessageResponseDto>()
                .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src => src.Sender != null ? src.Sender.UserName : null));

            CreateMap<Order, OrderDialogResponseDto>()
                .ForMember(dest => dest.OrderId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.OrderTitle, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.UserName : null))
                .ForMember(dest => dest.ExecutorName, opt => opt.MapFrom(src => src.Executor != null ? src.Executor.UserName : null))
                .ForMember(dest => dest.LastMessage, opt => opt.MapFrom(src => src.OrderMessages.OrderByDescending(m => m.SentAt).FirstOrDefault()))
                .ForMember(dest => dest.UnreadCount, opt => opt.MapFrom((src, dest, destMember, context) =>
                {
                    var currentUserId = context.Items.TryGetValue("CurrentUserId", out var idObj) ? idObj?.ToString() : null;
                    return !string.IsNullOrEmpty(currentUserId)
                        ? src.OrderMessages.Count(m => !m.IsRead && m.SenderId != currentUserId)
                        : 0;
                }));
        }
    }
}