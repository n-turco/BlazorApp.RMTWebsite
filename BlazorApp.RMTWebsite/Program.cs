using BlazorApp.RMTWebsite.Components;
using BlazorApp.RMTWebsite.Models;
using BlazorApp.RMTWebsite.RMTServices;
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
            builder.Services.AddHttpClient<EmailInquiry>();
            builder.Services.AddScoped<IEmailSender, EmailSenderService>();
            

            //bind Email Settings from appsettings to the EmailSettings class
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

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
