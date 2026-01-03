namespace Shop_ProjForWeb.Tests.Helpers;

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;

public static class ApiTestHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<T?> PostAsync<T>(HttpClient client, string url, object data)
    {
        var response = await client.PostAsJsonAsync(url, data);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    public static async Task<T?> GetAsync<T>(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    public static async Task<HttpResponseMessage> PutAsync(HttpClient client, string url, object data)
    {
        return await client.PutAsJsonAsync(url, data);
    }

    public static async Task<HttpResponseMessage> DeleteAsync(HttpClient client, string url)
    {
        return await client.DeleteAsync(url);
    }

    public static void AssertSuccessStatusCode(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            $"Expected success status code but got {response.StatusCode}");
    }

    public static void AssertStatusCode(HttpResponseMessage response, HttpStatusCode expected)
    {
        response.StatusCode.Should().Be(expected,
            $"Expected {expected} but got {response.StatusCode}");
    }

    public static async Task<T?> GetResponseAsync<T>(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }
}
