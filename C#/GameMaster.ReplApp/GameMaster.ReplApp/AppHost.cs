using System;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaster.ReplApp
{
    public static class AppHost
    {
        public static IServiceProvider Services { get; set; } = new ServiceCollection().BuildServiceProvider();
    }
}
