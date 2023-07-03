using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json;

public class SlashCommandHandler
{
    private readonly DiscordSocketClient _client;
    private readonly iKEDOClient _kedoClient;
    private readonly UserLoginClient _userLoginClient;
    public SlashCommandHandler(DiscordSocketClient client, iKEDOClient kedoClient, UserLoginClient userLoginClient)
    {
        _client = client;
        _kedoClient = kedoClient;
        _userLoginClient = userLoginClient;
    }
    //билдинг слеш-команд
    public async Task Slash_Ready()
    {
        //лист забилденных слеш-команд
        List<ApplicationCommandProperties> applicationCommandProperties = new();
        //билдер глобальных (используемых где-угодно) слеш-комманд
        List<SlashCommandBuilder> globalCommands = new List<SlashCommandBuilder>()
        {
            new SlashCommandBuilder()
            .WithName("help")
            .WithDescription("Получить список доступных команд бота"),
            new SlashCommandBuilder()
            .WithName("last-notification")
            .WithDescription("Последнее уведомление от iКЭДО"),
            new SlashCommandBuilder()
            .WithName("register")
            .WithDescription("Регистрация пользователя и его номера телефона")
            .AddOption("phone", ApplicationCommandOptionType.String, "Ваш номер телефона. Записывать в таком ужатом формате: 79991234567", isRequired: true),
            new SlashCommandBuilder()
            .WithName("restriction-test")
            .WithDescription("Проверка регистрации"),
            new SlashCommandBuilder()
            .WithName("change-phone")
            .WithDescription("Замена вашего телефона")
            .AddOption("phone", ApplicationCommandOptionType.String, "Ваш номер телефона. Записывать в таком ужатом формате: 79991234567", isRequired: true),
            new SlashCommandBuilder()
            .WithName("get-notifications")
            .WithDescription("Включить/отключить опцию автоматической отправки уведомлений вам в личные сообщения")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("toggle")
                .WithDescription("Включить/выключить (true/false)")
                .WithRequired(true)
                .AddChoice("отключить", 0)
                .AddChoice("включить", 1)
                .WithType(ApplicationCommandOptionType.Boolean)
            ),
            new SlashCommandBuilder()
            .WithName("clear-notifications")
            .WithDescription("Очистка всех имеющихся уведомлений, что у вас есть при вызове /last-notification")
        };
        //попытка забилдить
        try
        {
            foreach (SlashCommandBuilder gcb in globalCommands) applicationCommandProperties.Add(gcb.Build());
            //очищаются все команды и ставятся те команды, которые нужны
            await _client.BulkOverwriteGlobalApplicationCommandsAsync(applicationCommandProperties.ToArray());
        }
        catch (HttpException ex)
        {
            var json = JsonConvert.SerializeObject(ex.Errors, Formatting.Indented);
            Console.WriteLine(json);
        }
    }
    //слеш-команды и их функции
    public async Task SlashCommandExecute(SocketSlashCommand command)
    {
        switch (command.Data.Name)
        {
            case "help":
                await HelpCommand(command);
                break;
            case "last-notification":
                await GetRequestCommand(command);
                break;
            case "register":
                await RegisterCommand(command);
                break;
            case "restriction-test":
                await TestRestriction(command);
                break;
            case "change-phone":
                await ChangePhoneCommand(command);
                break;
            case "get-notifications":
                await SetNotificationsPingsCommand(command);
                break;
            case "clear-notifications":
                await ClearNotificationsCommand(command);
                break;
            default:
                await command.RespondAsync($"You executed {command.Data.Name}");
                break;
        }
    }
    //меню-команды и их функции
    public async Task MenuOptionExecute(SocketMessageComponent choice)
    {
        switch (choice.Data.CustomId)
        {
            case "obj-choose":
                SendChosenNotification(choice);
                break;
        }
    }
    //меню-команда отправки последнего уведомления из списка имеющихся документов
    private async Task SendChosenNotification(SocketMessageComponent choice)
    {
        var text = string.Join(", ", choice.Data.Values);
        var embedBuilder = new EmbedBuilder()
            .WithAuthor("iКЭДО")
            .WithTitle("Уведомление")
            .WithDescription(text)
            .WithColor(new Color(138, 106, 226))
            .WithCurrentTimestamp();
        await choice.RespondAsync(embed: embedBuilder.Build());
    }
    private async Task HelpCommand(SocketSlashCommand command)
    {
        string response = "";
        foreach (RestGlobalCommand cmd in await _client.Rest.GetGlobalApplicationCommands())
        {
            response += $"/{cmd.Name} - {cmd.Description}\n";
        }
        var embedBuilder = new EmbedBuilder()
            .WithAuthor("iКЭДО")
            .WithTitle("Команды")
            .WithDescription(response)
            .WithColor(new Color(138, 106, 226))
            .WithCurrentTimestamp();
        await command.RespondAsync(embed: embedBuilder.Build());
    }
    //получения списка уведомлений для получения их последнего уведомления
    private async Task GetRequestCommand(SocketSlashCommand command)
    {
        if (await _userLoginClient.FindUser(command.User.Id))
        {
            if (_userLoginClient.UserDatas.Find(x => x.UserID == command.User.Id).Entities.Count != 0)
            {
                List<SystemEvents> se = await _kedoClient.GetNotificationRequest();
                var menuBuilder = new SelectMenuBuilder()
                    .WithPlaceholder("Выберите объект")
                    .WithCustomId("obj-choose")
                    .WithMinValues(1)
                    .WithMaxValues(1);
                foreach (EntityDataSet entity in _userLoginClient.UserDatas.Find(x => x.UserID == command.User.Id).Entities)
                {
                    menuBuilder.AddOption($"{entity.Name} {entity.DateTime}", $"Последнее уведомление по этому объекту:\n{se.FindAll(x => x.EntityId == entity.Id).Last().SystemEventType}\nв: {se.FindAll(x => x.EntityId == entity.Id).Last().EventTime}", $"{entity.Type}");
                }
                var componentBuilder = new ComponentBuilder()
                    .WithSelectMenu(menuBuilder);
                await command.RespondAsync("Выберите объект, у которого вы хотите узнать последнее оповещение", components: componentBuilder.Build());
            }
            else await command.RespondAsync("На данный момент у вас нет активных объектов (документов или новое рабочее место).\nP.S: если вы только начали пользоваться ботом, оповещения вы будете получать, если вы начнете пользоваться iКЭДО уже после начала работы с ботом.");
        }
        else await command.RespondAsync("Вы не зарегистрированы!");
    }
    //регистрация
    private async Task RegisterCommand(SocketSlashCommand command)
    {
        await command.RespondAsync(await _userLoginClient.AddUser(command.User.Id, $"{command.Data.Options.First().Value}"));
    }
    //проверка регистрации
    private async Task TestRestriction(SocketSlashCommand command)
    {

        if(await _userLoginClient.FindUser(command.User.Id))
        {
            await command.RespondAsync("Вы зарегистрированы!");
        }
        else
        {
            await command.RespondAsync("Вы не зарегистрированы!");
        }
    }
    //смена телефона
    private async Task ChangePhoneCommand(SocketSlashCommand command)
    {
        await command.RespondAsync(await _userLoginClient.ChangePhoneNumber(command.User.Id, $"{command.Data.Options.First().Value}"));
    }
    //переключатель автоматической отправки уведомлений
    private async Task SetNotificationsPingsCommand(SocketSlashCommand command)
    {
        await command.RespondAsync(await _userLoginClient.SetNotificationPings(command.User.Id, Convert.ToBoolean(command.Data.Options.First().Value)));
    }
    private async Task ClearNotificationsCommand(SocketSlashCommand command)
    {
        await command.RespondAsync(await _userLoginClient.ClearEntities(command.User.Id));
    }
}