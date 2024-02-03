﻿using CryptoExchange.Net.Clients;
using Mexc.Net.Clients;
using Mexc.Net.Interfaces;
using Mexc.Net.Interfaces.Clients;
using Mexc.Net.Objects.Options;
using Mexc.Net.SymbolOrderBooks;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Net.Http;

namespace Mexc.Net
{
    /// <summary>
    /// Helper functions
    /// </summary>
    public static class MexcHelpers
    {
        /// <summary>
        /// Add the IMexcRestClient and IMexcSocketClient to the sevice collection so they can be injected
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="defaultRestOptionsDelegate">Set default options for the rest client</param>
        /// <param name="defaultSocketOptionsDelegate">Set default options for the socket client</param>
        /// <param name="socketClientLifeTime">The lifetime of the IMexcSocketClient for the service collection. Defaults to Singleton.</param>
        /// <returns></returns>
        public static IServiceCollection AddMexc(
            this IServiceCollection services,
            Action<MexcRestOptions>? defaultRestOptionsDelegate = null,
            Action<MexcSocketOptions>? defaultSocketOptionsDelegate = null,
            ServiceLifetime? socketClientLifeTime = null)
        {
            var restOptions = MexcRestOptions.Default.Copy();

            if (defaultRestOptionsDelegate != null)
            {
                defaultRestOptionsDelegate(restOptions);
                MexcRestClient.SetDefaultOptions(defaultRestOptionsDelegate);
            }

            if (defaultSocketOptionsDelegate != null)
                MexcSocketClient.SetDefaultOptions(defaultSocketOptionsDelegate);

            services.AddHttpClient<IMexcRestClient, MexcRestClient>(options =>
            {
                options.Timeout = restOptions.RequestTimeout;
            }).ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();
                if (restOptions.Proxy != null)
                {
                    handler.Proxy = new WebProxy
                    {
                        Address = new Uri($"{restOptions.Proxy.Host}:{restOptions.Proxy.Port}"),
                        Credentials = restOptions.Proxy.Password == null ? null : new NetworkCredential(restOptions.Proxy.Login, restOptions.Proxy.Password)
                    };
                }
                return handler;
            });

            services.AddTransient<ICryptoRestClient, CryptoRestClient>();
            services.AddTransient<ICryptoSocketClient, CryptoSocketClient>();
            services.AddSingleton<IMexcOrderBookFactory, MexcOrderBookFactory>();
            services.AddTransient(x => x.GetRequiredService<IMexcRestClient>().SpotApi.CommonSpotClient);
            if (socketClientLifeTime == null)
                services.AddSingleton<IMexcSocketClient, MexcSocketClient>();
            else
                services.Add(new ServiceDescriptor(typeof(IMexcSocketClient), typeof(MexcSocketClient), socketClientLifeTime.Value));
            return services;
        }
    }
}
