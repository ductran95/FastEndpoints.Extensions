using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication2.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetWeatherForecast/{id}")]
    public IEnumerable<WeatherForecast> Get([FromRoute] int id, [FromQuery] IEnumerable<string> name)
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }
    
    [HttpPost(Name = "Post/{id}")]
    public IEnumerable<WeatherForecast> Post([FromRoute] int id, [FromBody] PostBody data)
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }
}

public class PostBody
{
    public string cID { get; set; }

    public string? CreatedBy { get; set; }

    /// <summary>
    /// the name of the cutomer goes here
    /// </summary>
    public string? CustomerName { get; set; }

    public IEnumerable<string> PhoneNumbers { get; set; }

    [JsonIgnore]
    public bool HasCreatePermission { get; set; }
}