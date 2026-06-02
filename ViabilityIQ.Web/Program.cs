using Microsoft.AspNetCore.Components.Server;
using ViabilityIQ.Infrastructure.Extensions;
using ViabilityIQ.Web.Components;
using ViabilityIQ.Web.Extensions;

namespace ViabilityIQ.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents().AddInteractiveServerComponents();
            builder.Services.Configure<CircuitOptions>(options =>
            {
                options.DetailedErrors = true;
            });

            builder.Services.AddWebServices();  //Located in the Web projects
            builder.Services.AddInfrastructureServices();    //Located in the Infrastructure project


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
