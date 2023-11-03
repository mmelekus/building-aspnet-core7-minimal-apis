namespace DishesAPI.Models;

public record class DishDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
}
