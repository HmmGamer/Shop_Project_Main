namespace Shop_ProjForWeb.Core.Application.Services;

using System.Text.Json;

public class AgifyService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AgifyService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<int?> GetPredictedAgeAsync(string name)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"https://api.agify.io?name={name}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(content);
            var root = jsonDocument.RootElement;

            if (root.TryGetProperty("age", out var ageElement) && ageElement.TryGetInt32(out var age))
            {
                return age;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
