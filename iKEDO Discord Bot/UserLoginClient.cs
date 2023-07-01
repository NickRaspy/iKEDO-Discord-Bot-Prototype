﻿using System.Text.Json;

public class UserLoginClient
{
    private readonly iKEDOClient _kedoclient;
    public List<UserData> UserDatas;
    public UserLoginClient(iKEDOClient kedoclient)
    {
        UserDatas = GetUserDatas();
        _kedoclient = kedoclient;
    }
    //регистрация пользователя
    public async Task<string> AddUser(ulong userID, string phoneNumber)
    {
        string response;
        if(await FindUser(userID))
        {
            response = "Вы уже зарегистрированы в нашей системе!";
        }
        else
        {
            //поиск номера телефона среди зарегистрированных в iКЭДО
            if(await _kedoclient.FindEmployeeByPhoneRequest(phoneNumber))
            {
                //поиск номера телефона среди зарегистрированных в боте
                if(UserDatas.Contains(UserDatas.Find(x => x.Phone == phoneNumber)))
                {
                    response = "Данный номер телефона уже используется другим пользователем";
                }
                else
                {
                    //новый пользователь
                    UserDatas.Add(new UserData { UserID = userID, Phone = phoneNumber, Docs = new List<Documents>() });
                    SetUserDatas(UserDatas);
                    response = "Вы успешно зарегистрированы!";
                }
            }
            else
            {
                response = "Данный номер телефона не обнаружен";
            }
        }
        return response;
    }
    //поиск пользователя среди зарегистрированных в боте
    public async Task<bool> FindUser(ulong userID)
    {
        if(UserDatas != null)
        {
            foreach(UserData userData in UserDatas)
            {
                if(userData.UserID == userID)
                {
                    return true;
                }
            }
            return false;
        }
        return false;
    }
    //смена номера телефона
    public async Task<string> ChangePhoneNumber(ulong userID, string phoneNumber)
    {
        string response;
        if(await FindUser(userID))
        {
            //поиск номера телефона среди зарегистрированных в боте
            if (!UserDatas.Contains(UserDatas.Find(x => x.Phone == phoneNumber)))
            {
                //поиск номера телефона среди зарегистрированных в iКЭДО
                if (await _kedoclient.FindEmployeeByPhoneRequest(phoneNumber))
                {
                    UserDatas.Find(x => x.UserID == userID).Phone = phoneNumber;
                    SetUserDatas(UserDatas);
                    response = "Ваш телефон заменен";
                }
                else
                {
                    response = "Данный номер телефона не обнаружен";
                }
            }
            else
            {
                response = "Данный номер телефона уже используется.";
            }
        }
        else response = "Вы не зарегистрированы!";
        return response;
    }
    //переключатель автоматической отправки сообщений
    public async Task<string> SetNotificationPings(ulong userID, bool getPing)
    {
        string response;
        if (await FindUser(userID))
        {
            bool ping = UserDatas.Find(x => x.UserID == userID).GetNotificationPing;
            if (ping == getPing)
            {
                if (ping)
                {
                    response = "У вас уже включено автоматическое получение уведомления";
                }
                else
                {
                    response = "У вас уже отключено автоматическое получение уведомления";
                }
            }
            else
            {
                UserDatas.Find(x => x.UserID == userID).GetNotificationPing = getPing;
                if (getPing)
                {
                    response = "Вы включили автоматическое получение уведомления";
                }
                else
                {
                    response = "Вы отключили автоматическое получение уведомления";
                }
            }
            SetUserDatas(UserDatas);
        }
        else response = "Вы не зарегистрированы!";
        return response;
    }
    //добавление документа, по которому можно будет сверять обновления, к пользователю
    public async Task AddDocument(string phone, Documents document)
    {
        UserDatas.Find(x => x.Phone == phone).Docs.Add(document);
        SetUserDatas(UserDatas);
    }
    //получение данных пользователей бота
    public static List<UserData> GetUserDatas()
    {
        List<UserData> data = new List<UserData>();
        string path = AppDomain.CurrentDomain.BaseDirectory + "/users.json";
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            data = JsonSerializer.Deserialize<List<UserData>>(json);
        }
        return data;
    }
    //установка новых значений данных пользователя бота
    static void SetUserDatas(List<UserData> data)
    {
        string path = AppDomain.CurrentDomain.BaseDirectory + "/users.json";
        string json = JsonSerializer.Serialize(data);
        File.WriteAllText(path, json);
    }
}
//данные пользователя бота
public class UserData
{
    public ulong UserID { get; set; }
    public string Phone { get; set; }
    public List<Documents> Docs { get; set; }
    public bool GetNotificationPing { get; set; }
} 