namespace Api.Tests;

using System.Text.Json;
using System.Text.Json.Serialization;

public static class HttpContentExtensions
{
    static readonly JsonSerializerOptions s_options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public static async Task<T> ReadAsAsyncWithAssertion<T>(this HttpContent content)
    {
        var str = await content.ReadAsStringAsync();
        str.Should().NotBeNull();

        var t = JsonSerializer.Deserialize<T>(str, s_options);
        if (t == null)
            throw new InvalidOperationException("Failed to deserialize");

        return t;
    }

    public static Task<T> ReadAsExample<T>(this HttpContent content, T example)
        => content.ReadAsAsyncWithAssertion<T>();
}
