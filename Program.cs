using System.Text.Json;
using GmailHandler.Services;
using GmailHandler.Services.GeminiImplementation;
using GmailHandler.Services.Implementation1;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GmailHandler
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
           
            builder.Services.AddSingleton<IGmailServiceWrapper,GmailServiceWrapper>();
           
            builder.Services.AddSingleton<IAuthenticateService, AuthenticateService>();

            builder.Services.AddSingleton<IAiService, AiService>();

            builder.Services.AddHttpContextAccessor();
            // Add memory cache
            builder.Services.AddMemoryCache();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.Map("summary", async (HttpContext context, IAiService aiservice) =>
            {
                string text = aiservice.summary("you have follow 5 roles from our website you can visit our website and use folowing 5 roles for it. you can apply on it");
                return Results.Ok(text);
            });
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
