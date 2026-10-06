using BlazorApp.RMTWebsite.Components;
using BlazorApp.RMTWebsite.Services;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace BlazorApp.RMTWebsite
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents();
            //  builder.Services.AddRazorComponents().AddInteractiveServerComponents();
            builder.Services.AddHttpClient<IEmailSender, EmailSenderService>();
            // Treatments and prices, shared by the Home preview and the Services page (plan 2.2)
            builder.Services.AddSingleton<IServiceCatalog, ServiceCatalog>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>();

            app.Run();
        }
    }
}
