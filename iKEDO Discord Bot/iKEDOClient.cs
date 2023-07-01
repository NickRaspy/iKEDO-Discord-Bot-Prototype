using System.Text.Json;
using System.Net.Http.Headers;

public class iKEDOClient
{
    private readonly HttpClient _httpClient;
    public int statusCode;
    public List<SystemEvents> events;
    public iKEDOClient()
    {
        _httpClient = httpClient(GetBearerToken());
    }
    //проверка соединения 
    public async Task TestConnection()
    {
        HttpResponseMessage response = await _httpClient.GetAsync("notifications/SystemEvents?Offset=1000&Count=2000");
        response.EnsureSuccessStatusCode();
    }
    //GET-запрос списка уведомлений
    public async Task GetNotificationRequest()
    {
        HttpResponseMessage response = await _httpClient.GetAsync("notifications/SystemEvents?Offset=1000&Count=2000");
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        events = JsonSerializer.Deserialize<List<SystemEvents>>(jsonResponse);
    }
    //GET-запрос данных о документе
    public async Task<Documents> GetDocumentRequest(string documentID)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"docstorage/Documents/{documentID}");
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        Documents dc = JsonSerializer.Deserialize<Documents>(jsonResponse);
        return dc;
    }
    //GET-запрос данных о сотруднике (пользователе iКЭДО)
    public async Task<KEDOUser> GetEmployeeRequest(string kedouserID)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"staff/Employees/{kedouserID}");
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        KEDOUser kuser = JsonSerializer.Deserialize<KEDOUser>(jsonResponse);
        return kuser;
    }
    //поиск пользователя по телефону среди списка всех сотрудников (пользователей iКЭДО)
    public async Task<bool> FindEmployeeByPhoneRequest(string phoneNumber)
    {
        HttpResponseMessage response = await _httpClient.GetAsync("staff/Employees");
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        List<KEDOUser> kusers = JsonSerializer.Deserialize<List<KEDOUser>>(jsonResponse);
        if (kusers.Contains(kusers.Find(x => x.PhoneNumber == phoneNumber))) return true;
        else return false;
    }
    //клиент iКЭДО
    static HttpClient httpClient(string token)
    {
        HttpClient httpClient = new();
        var url = @"https://api-gw.kedo-demo.cloud.astral-dev.ru/api/v3/";
        httpClient.BaseAddress = new Uri(url);
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpClient.DefaultRequestHeaders.Add("kedo-gateway-token-type", "IntegrationApi");
        return httpClient;
    }
    //получение токена из локального файла
    static string GetBearerToken()
    {
        Tokens newTokens;
        string path = AppDomain.CurrentDomain.BaseDirectory + "/tokens.json";
        string json = File.ReadAllText(path);
        newTokens = JsonSerializer.Deserialize<Tokens>(json);
        return newTokens.Bearer;
    }
}
//данные уведомления
public class SystemEvents
{
    public string EntityId { get; set; }
    public DateTime EventTime { get; set; }
    public string SystemEventType { get; set; }
}
//данные о сотруднике (пользователе iКЭДО)
//P.S: 2 PhoneNumber - костыль, ибо staff/Employees и staff/Employees/{id} имели разные способы хранения номера телефона
public class KEDOUser
{
    public string Id { get; set; }
    public string PhoneNumber { get; set; }
    public List<ContactsData> Contacts { get; set; }
    public class ContactsData
    {
        public string PhoneNumber { get; set; }
    }
}
//данные документа
public class Documents
{
    public string Name { get; set; }
    public string CreatorId { get; set; }
    public DateTime CreationTime { get; set; }
    public string Id { get; set; }
    public string DocumentType { get; set; }
}

