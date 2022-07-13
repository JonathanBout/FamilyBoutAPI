using Microsoft.AspNetCore.HttpOverrides;
using Newtonsoft.Json;
using System.Net;
using System.Web;

namespace FamilyBoutAPI
{
    public class Program
    {
        static int views = 0;
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

            app.UseAuthorization();
            
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
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

            app.MapGet("/addview", () =>
            {
                views++;
                return new { views };
            });

            app.MapPost("/getcode", async (HttpRequest rq) =>
            {
                string body = "";
                using (StreamReader stream = new StreamReader(rq.Body))
                {
                    body = await stream.ReadToEndAsync();
                }

                int? userCode = null;
                dynamic? jsonObj = JsonConvert.DeserializeObject<dynamic>(body);

                if (jsonObj is not null)
                {
                    int nameCode = 1;
                    if (jsonObj.username is string username)
                    {
                        for (int i = 0; i < username.Length; i++)
                        {
                            char c = username[i];
                            nameCode *= c + i;
                        }
                    }
                    int passwordCode = 1;
                    if (jsonObj.password is string password)
                    {
                        for (int i = 0; i < password.Length; i++)
                        {
                            char c = password[i];
                            nameCode *= c + i;
                        }
                    }
                    userCode = nameCode + passwordCode;
                }

                return new { userCode };
            });

            app.Run();
        }
    }
}