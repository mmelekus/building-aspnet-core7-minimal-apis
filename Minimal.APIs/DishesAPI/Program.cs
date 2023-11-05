using System.Security.Claims;
using AutoMapper;
using DishesAPI.DbContexts;
using DishesAPI.Entities;
using DishesAPI.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DishesDbContext>(o => o.UseSqlite(builder.Configuration["ConnectionStrings:DishesDBConnectionString"]));

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var dishesEndpoints = app.MapGroup("/dishes");
var dishWithGuidEndpoints = dishesEndpoints.MapGroup("/{dishId:guid}");
var ingredientsEndpoints = dishWithGuidEndpoints.MapGroup("/ingredients");

dishesEndpoints.MapGet("", async Task<Ok<IEnumerable<DishDto>>> (DishesDbContext dishesDbContext,
    ClaimsPrincipal claimsPrincipal,
    IMapper mapper,
    string? name) =>
    {
        Console.WriteLine($"User authenticated? {claimsPrincipal.Identity?.IsAuthenticated}");

        return TypedResults.Ok(mapper.Map<IEnumerable<DishDto>>(await dishesDbContext.Dishes
            .Where(d => name == null || d.Name.Contains(name))
            .ToListAsync()));
    });

dishWithGuidEndpoints.MapGet("", async Task<Results<NotFound, Ok<DishDto>>> (DishesDbContext dishesDbContext,
    IMapper mapper,
    Guid dishId) =>
    {
        var dishEntity = await dishesDbContext.Dishes
            .FirstOrDefaultAsync(d => d.Id == dishId);
        if (dishEntity == null) { return TypedResults.NotFound(); }

        return TypedResults.Ok(mapper.Map<DishDto>(dishEntity));
    }).WithName("GetDish");

dishesEndpoints.MapGet("/{dishName}", async Task<Ok<DishDto>> (DishesDbContext dishesDbContext,
    IMapper mapper,
    string dishName) =>
    {
        return TypedResults.Ok(mapper.Map<DishDto>(await dishesDbContext.Dishes
            .FirstOrDefaultAsync(d => d.Name == dishName)));
    });

ingredientsEndpoints.MapGet("", async Task<Results<NotFound, Ok<IEnumerable<IngredientDto>>>> (DishesDbContext dishesDbContext,
    IMapper mapper,
    Guid dishId) =>
    {
        var dishEntity = await dishesDbContext.Dishes
            .FirstOrDefaultAsync(d => d.Id == dishId);
        if (dishEntity == null) { return TypedResults.NotFound(); }

        return TypedResults.Ok(mapper.Map<IEnumerable<IngredientDto>>((await dishesDbContext.Dishes
            .Include(d => d.Ingredients)
            .FirstOrDefaultAsync(d => d.Id == dishId))?.Ingredients));
    });

dishesEndpoints.MapPost("", async Task<CreatedAtRoute<DishDto>> (DishesDbContext dishesDbContext,
    IMapper mapper,
    // LinkGenerator linkGenerator,
    // HttpContext httpContext,
    DishForCreationDto dishForCreationDto) =>
    {
        var dishEntity = mapper.Map<Dish>(dishForCreationDto);
        dishesDbContext.Add(dishEntity);
        await dishesDbContext.SaveChangesAsync();

        var dishToReturn = mapper.Map<DishDto>(dishEntity);
        return TypedResults.CreatedAtRoute(
            dishToReturn,
            "GetDish",
            new { dishId = dishToReturn.Id }
        );

        // var linkToDish = linkGenerator.GetUriByName(
        //     httpContext,
        //     "GetDish",
        //     new { dishId = dishToReturn.Id });
        // return TypedResults.Created(linkToDish, dishToReturn);
    });

dishWithGuidEndpoints.MapPut("", async Task<Results<NotFound, NoContent>> (DishesDbContext dishesDbContext,
    IMapper mapper,
    Guid dishId,
    DishForUpdateDto dishForUpdateDto) =>
    {
        var dishEntity =  await dishesDbContext.Dishes
            .FirstOrDefaultAsync(d => d.Id == dishId);
        if (dishEntity is null) { return TypedResults.NotFound(); }

        mapper.Map(dishForUpdateDto, dishEntity);

        await dishesDbContext.SaveChangesAsync();

        return TypedResults.NoContent();
    });

dishWithGuidEndpoints.MapDelete("", async Task<Results<NotFound, NoContent>> (DishesDbContext dishesDbContext,
    Guid dishId) =>
    {
        var dishEntity = await dishesDbContext.Dishes
            .FirstOrDefaultAsync(d => d.Id == dishId);
        if (dishEntity is null) { return TypedResults.NotFound(); }

        dishesDbContext.Dishes.Remove(dishEntity);
        await dishesDbContext.SaveChangesAsync();

        return TypedResults.NoContent();
    });
    
// recreate & migrate the database on each run, for demo purposes
using (var serviceScope = app.Services.GetService<IServiceScopeFactory>().CreateScope())
{
    var context = serviceScope.ServiceProvider.GetRequiredService<DishesDbContext>();
    context.Database.EnsureDeleted();
    context.Database.Migrate();
}

app.Run();

// var summaries = new[]
// {
//     "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
// };

// app.MapGet("/weatherforecast", () =>
// {
//     var forecast =  Enumerable.Range(1, 5).Select(index =>
//         new WeatherForecast
//         (
//             DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//             Random.Shared.Next(-20, 55),
//             summaries[Random.Shared.Next(summaries.Length)]
//         ))
//         .ToArray();
//     return forecast;
// })
// .WithName("GetWeatherForecast")
// .WithOpenApi();

// record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
// {
//     public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
// }
