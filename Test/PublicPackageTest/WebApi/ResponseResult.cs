using System.Text.Json.Serialization;

namespace PublicPackageTest.WebApi;

public class ResponseResult<T>
{
    [JsonPropertyName("stateCode")]
    public int StateCode { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("data")]
    public T Data { get; set; }
}
