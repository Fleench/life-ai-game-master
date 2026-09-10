using System;
using System.Net.Http;

namespace GameMaster.Client;

public static class GameMasterClientFactory
{
    public static IGameMasterClient CreateHttpClient(string baseAddress, string apiKey)
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(baseAddress) };
        return new HttpGameMasterClient(httpClient, apiKey);
    }

#if ANDROID
    public static IGameMasterClient CreateAndroidBinderClient(Android.Content.Context context)
    {
        return new AndroidBinderGameMasterClient(context);
    }
#endif
}
