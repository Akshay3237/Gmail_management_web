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

            builder.Services.AddSingleton<IUniqueSenderService, UniqueSenderService>();
            
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

            app.Map("checkinguniquesenders", async (HttpContext context,IUniqueSenderService uniqueSender) =>
            {
                List<Models.UniqueSenderModel> senders=  uniqueSender.GetUniqueSenders(20);

                return Results.Ok(senders);
            });
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
