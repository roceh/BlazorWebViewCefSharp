using BlazorApp.CefSharp.Services;
using BlazorApp.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorApp.CefSharp
{
    public class StartupCefSharp : Startup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddSingleton<IWindowService, WindowService>();
        }
    }
}
