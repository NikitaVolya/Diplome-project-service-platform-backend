using API.DTO.OrderMessage;
using AutoMapper;
using BLL.Results;
using Domain.Entities;
using Domain.Models;

namespace API.Mapping
{
    public class OrderMessageProfile : Profile
    {
        public OrderMessageProfile()
        {
            CreateMap<OrderMessage, OrderMessageResponseDto>()
                .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src => src.Sender != null ? src.Sender.UserName : null))
                .ForMember(dest => dest.ReceiverName, opt => opt.MapFrom(src => src.Receiver != null ? src.Receiver.UserName : null));

            CreateMap<OrderDialog, OrderDialogResponseDto>()
                .ForMember(dest => dest.OrderId, opt => opt.MapFrom(src => src.Order.Id))
                .ForMember(dest => dest.OrderTitle, opt => opt.MapFrom(src => src.Order.Title))
                .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.Customer.Id))
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.UserName))
                .ForMember(dest => dest.ExecutorId, opt => opt.MapFrom(src => src.Executor.Id))
                .ForMember(dest => dest.ExecutorName, opt => opt.MapFrom(src => src.Executor.UserName))
                .ForMember(dest => dest.ParticipantId, opt => opt.MapFrom((src, dest, destMember, context) => GetParticipant(src, context).Id))
                .ForMember(dest => dest.ParticipantName, opt => opt.MapFrom((src, dest, destMember, context) => GetParticipant(src, context).UserName))
                .ForMember(dest => dest.LastMessage, opt => opt.MapFrom(src => src.LastMessage))
                .ForMember(dest => dest.UnreadCount, opt => opt.MapFrom(src => src.UnreadCount));
        }

        // "The other side" depends on who is looking: the customer sees the executor and vice versa.
        // The controller passes the current user through the mapping context.
        private static ApplicationUser GetParticipant(OrderDialog dialog, ResolutionContext context)
        {
            var currentUserId = context.Items.TryGetValue("CurrentUserId", out var idObj) ? idObj?.ToString() : null;

            return currentUserId == dialog.Customer.Id ? dialog.Executor : dialog.Customer;
        }
    }
}
