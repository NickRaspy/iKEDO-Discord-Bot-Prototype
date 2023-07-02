using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using System.Text.Json;

public class Program
{
    private readonly IServiceProvider _services;
    private readonly Tokens _tokens;
    public Program()
    {
        _tokens = SetTokens();
        _services = CreateServices();
    }
    static void Main(string[] args) => new Program().RunAsync(args).GetAwaiter().GetResult();
    //Dependency Injection
    static IServiceProvider CreateServices()
    {
        var config = new DiscordSocketConfig()
        {
            
        };
        var collection = new ServiceCollection()
            .AddSingleton(config)
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton<iKEDOClient>()
            .AddSingleton<SlashCommandHandler>()
            .AddSingleton<UserLoginClient>()
            .AddSingleton<NotificationUpdater>();

        return collection.BuildServiceProvider();
    }
    async Task RunAsync(string[] args)
    {
        var client = _services.GetRequiredService<DiscordSocketClient>();
        var kedoClient = _services.GetRequiredService<iKEDOClient>();
        var notificationUpdater = _services.GetRequiredService<NotificationUpdater>();
        var slashHandler = _services.GetRequiredService<SlashCommandHandler>();
        //проверка подключения
        try
        {
            await kedoClient.TestConnection();
        }
        catch (HttpRequestException hpe)
        {
            Console.WriteLine($"Обнаружена ошибка при подключении к серверу:{hpe.StatusCode}");
            switch (hpe.StatusCode)
            {
                case System.Net.HttpStatusCode.Unauthorized:
                    Console.WriteLine("Судя по данной ошибке, ваш токен либо просрочен, либо записан неправильно. Попробуйте удалить или редактировать файл tokens и запустить программу заново.");
                    break;
                case System.Net.HttpStatusCode.Forbidden:
                    Console.WriteLine("У вас нет доступа к данным. Скорее всего, ваш токен не подходит для получения этих данных. Токен можно заменить, удалив или отредактировав файл tokens");
                    break;
                case System.Net.HttpStatusCode.NotFound:
                    Console.WriteLine("Сервис на данный момент откючен. Попробуйте запустить бота позднее.");
                    break;
            }
            Environment.Exit(0);
        }
        //логи подключения
        client.Log += async (msg) =>
        {
            await Task.CompletedTask;
            Console.WriteLine(msg);
        };
        //запуск задач после окончательной подготовки бота, таких как билдинг слеш комманд и рекурсивная проверка и высылка уведомлений
        client.Ready += slashHandler.Slash_Ready;
        client.Ready += notificationUpdater.UpdateNotification;
        //установка исполнителей, выполняющий слеш и меню комманды
        client.SelectMenuExecuted += slashHandler.MenuOptionExecute;
        client.SlashCommandExecuted += slashHandler.SlashCommandExecute;
        //подключение к боту дискорда
        await client.LoginAsync(TokenType.Bot, _tokens.Discord);
        await client.StartAsync();
        await Task.Delay(Timeout.Infinite);
    }
    //установка токенов
    //сохраняются в новый файл (гит игнор прописан)
    static Tokens SetTokens()
    {
        Tokens newTokens = new Tokens();
        string path = AppDomain.CurrentDomain.BaseDirectory + "/tokens.json";
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            newTokens = JsonSerializer.Deserialize<Tokens>(json);
        }
        else
        {
            Console.WriteLine("Insert Bearer Token"); newTokens.Bearer = Console.ReadLine();
            Console.WriteLine("Insert Discord Token"); newTokens.Discord = Console.ReadLine();
            string json = JsonSerializer.Serialize(newTokens);
            File.WriteAllText(path, json);
        }
        return newTokens;
    }
}
//токены
public class Tokens
{
    public string Bearer { get; set; }
    public string Discord { get; set; }
}