using API.DTO.Notification;
using AutoMapper;
using Domain.Models;

namespace API.Mapping
{
    public class NotificationProfile : Profile
    {
        public NotificationProfile()
        {
            CreateMap<Notification, NotificationResponseDto>();
        }
    }
}