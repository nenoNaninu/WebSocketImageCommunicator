using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace WebSocketImageCommunicator.WebSocketScript
{
    public static class WebSocketExtensions
    {
        public static IServiceCollection AddWebSocketHandler(this IServiceCollection services)
        {
            services.AddTransient<WebSocketObjectHolder>();

            foreach (var type in Assembly.GetEntryAssembly().ExportedTypes)
            {
                if (type.GetTypeInfo().BaseType == typeof(WebSocketHandler))
                {
                    services.AddSingleton(type);
                }
            }

            return services;
        }

        public static IApplicationBuilder MapWebSocketChatMiddleware(this IApplicationBuilder app, PathString path, WebSocketHandler handler)
        {
            return app.Map(path, _app => _app.UseMiddleware<WebSocketMiddleware>(handler));
        }
    }
}
