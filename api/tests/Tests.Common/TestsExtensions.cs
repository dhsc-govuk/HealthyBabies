using LanguageExt;
using Newtonsoft.Json;

namespace Tests.Common;

public static class TestsExtensions
{
    public static T RightOrThrow<TE, T>(this Either<TE, T> result) where TE : Exception
    {
        return result.Match<T>(
            r => r,
            _ => throw new ArgumentException("Value cannot be null"));
    }

    public static TE LeftOrThrow<TE, T>(this Either<TE, T> result) where TE : Exception
    {
        return result.Match<TE>(
            _ => throw new ArgumentException("Value cannot be null"),
            e => e);
    }

    public static async Task<T> ToResponseModel<T>(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<T>(content)
               ?? throw new ArgumentException("Response content cannot be null.");
    }
}