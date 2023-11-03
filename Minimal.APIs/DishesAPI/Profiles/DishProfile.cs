using AutoMapper;
using DishesAPI.Entities;
using DishesAPI.Models;
using Microsoft.VisualBasic;

namespace DishesAPI.Profiles;

public class DishProfile : Profile
{    
    public DishProfile()
    {
        CreateMap<Dish, DishDto>();
    }
}
