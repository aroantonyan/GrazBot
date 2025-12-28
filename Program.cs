using TelegramBot.Abstractions;
using TelegramBot.Configuration;
using TelegramBot.Helpers;
using TelegramBot.Infrastructure;
using TelegramBot.Infrastructure.Abstractions;
using TelegramBot.Services;
using TelegramBot.UI;
using TelegramBot.UI.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ITelegramUserRegistry, InMemoryTelegramUserRegistry>();
builder.Services.AddSingleton<IGrazSessionStore, InMemoryGrazSessionStore>();
builder.Services.AddSingleton<UserSeeder>();
builder.Services.AddSingleton<IGrazStore, InMemoryGrazStore>();
builder.Services.AddHttpClient<ITelegramApiClient, TelegramApiClient>();
builder.Services.AddHostedService<GrazSessionTimeoutWorker>();
builder.Services.AddSingleton<ITextProviderFactory, TextProviderFactory>();
builder.Services.AddSingleton<GrazUi>();
builder.Services.AddSingleton<IChatSettingsStore, InMemoryChatSettingsStore>();
builder.Services.AddSingleton<ITelegramUserTracker, TelegramUserTracker>();



builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddHttpClient();

builder.Services.Configure<TelegramOptions>(
    builder.Configuration.GetSection("Telegram"));

builder.Services.AddScoped<IBotService, BotService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var seeder = new UserSeeder(scope.ServiceProvider.GetRequiredService<ITelegramUserRegistry>());
    seeder.SeedUsers();
}


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();
app.Run();