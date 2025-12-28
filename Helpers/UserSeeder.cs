using TelegramBot.Abstractions;
using TelegramBot.Models;

namespace TelegramBot.Helpers;

public class UserSeeder(ITelegramUserRegistry userRegistry)
{
    private const long TestChatId = -5091803983; 

    public void SeedUsers()
    {
        var user1 = new TelegramUser
        {
            Id = 5523077561,
            FirstName = "Arman",
            Username = "mamyanarman",
            IsBot = false
        };

        var user2 = new TelegramUser
        {
            Id = 1806927529,
            FirstName = "Sargis",
            Username = "Sargis2002",
            IsBot = false
        };
        var user3 = new TelegramUser()
        {
            Id = 32432423,
            FirstName = "Vardan",
            Username = "Sargis2002",
            IsBot = false
        };
        var user4 = new TelegramUser()
        {
            Id = 32432423,
            FirstName = "Garnik",
            Username = "Sargis2002",
            IsBot = false
        };
        var user5 = new TelegramUser()
        {
            Id = 324322423,
            FirstName = "Mxo",
            Username = "Sargis2002",
            IsBot = false
        };
        var user6 = new TelegramUser()
        {
            Id = 32432112423,
            FirstName = "Suro",
            Username = "Sargis2002",
            IsBot = false
        };

        userRegistry.RegisterIfNeeded(TestChatId, user1);
        userRegistry.RegisterIfNeeded(TestChatId, user2);
        userRegistry.RegisterIfNeeded(TestChatId, user3);
        userRegistry.RegisterIfNeeded(TestChatId, user4);
        userRegistry.RegisterIfNeeded(TestChatId, user5);
        userRegistry.RegisterIfNeeded(TestChatId, user6);
    }
    
}

