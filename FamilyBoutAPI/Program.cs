using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

namespace FamilyBoutAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.KnownProxies.Add(IPAddress.Parse("10.0.0.100"));
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            //if (!app.Environment.IsDevelopment())
            //{
            //    app.UseHttpsRedirection();
            //}

            app.UseAuthorization();
            
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            //app.UseAuthentication();

            app.MapGet("/hehe", (string? x) =>
            {
                if (x is string value)
                {
                    return "Up: " + value?.ToUpper() + " Low: " + value?.ToLower();
                }
                return "Hehe :)";
            });


            app.Run();
        }
    }
}