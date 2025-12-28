using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TelegramBot.Abstractions;
using TelegramBot.Helpers;
using TelegramBot.Models;

namespace TelegramBot.Controllers;

[ApiController]
[Route("api/bot")]
public class BotController(IBotService botService, ILogger<BotController> logger, ITelegramUserRegistry userRegistry, UserSeeder userSeeder, IGrazStore grazStore) : ControllerBase
{
    [HttpPost("update")]
    public async Task<IActionResult> ReceiveUpdate([FromBody] JsonElement updateJson,
        CancellationToken cancellationToken)
    {
        var pretty = JsonSerializer.Serialize(
            updateJson,
            new JsonSerializerOptions { WriteIndented = true });

        logger.LogInformation(
            "===== TELEGRAM RAW UPDATE ====={NewLine}{Json}",
            Environment.NewLine,
            pretty);

        var update = updateJson.Deserialize<TelegramUpdate>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (update is null)
        {
            logger.LogWarning("TelegramUpdate deserialized as null");
            return Ok();
        }

        try
        {
            await botService.HandleUpdateAsync(update, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error while processing Telegram update.");
        }

        return Ok();
    }
    
    [HttpGet("users")]
    public IActionResult GetAllUsers()
    {
        var users = userRegistry.GetAll(-5091803983);
        return Ok(users);
    }

    [HttpPost("seedusers")]
    public IActionResult SeedUsers()
    {
        userSeeder.SeedUsers();
        return Ok();
    }

    [HttpGet("grazes")]
    public IActionResult GetGrazes()
    {
        var data = grazStore.GetAll(-5091803983);
        return Ok(data);
        
    }
}