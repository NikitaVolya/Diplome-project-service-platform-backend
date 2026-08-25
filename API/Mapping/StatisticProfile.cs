using API.DTO.Statistic;
using AutoMapper;
using Domain.Models;

namespace API.Mapping
{
    public class StatisticProfile : Profile
    {
        public StatisticProfile()
        {
            CreateMap<Statistic, StatisticResponseDto>();
        }
    }
}