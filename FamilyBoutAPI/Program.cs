using Microsoft.AspNetCore.HttpOverrides;
using Newtonsoft.Json;
using System.Net;
using System.Web;

namespace FamilyBoutAPI
{
    public class Program
    {
        static int views = 0;
        private protected static Dictionary<string, long> users = new Dictionary<string, long>();

        public static void Main(string[] args)
        {
            #region init
            var builder = WebApplication.CreateBuilder(args);

            AddDefault("Jorien", "Mirjam", "Maureen", "Robert Jan", "Anneloes", "Marilyn", "Jonathan", "Joëlle", "Noa");
            string userString = "";
            foreach (var user in users)
            {
                userString += $"Username: {user.Key,-15} Key: {user.Value:X}\n";
            }
            Console.WriteLine(userString);
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
            #endregion

            //Test endpoint
            app.MapGet("/hehe", (string? x) =>
            {
                if (x is string value)
                {
                    return "Up: " + value?.ToUpper() + " Low: " + value?.ToLower();
                }
                return "Hehe :)";
            });

            //View counter endpoint
            app.MapGet("/addview", (bool? addup) =>
            {
                if (addup is null || addup == true)
                    views++;
                return new { views };
            });

            //Login endpoint
            app.MapPost("/login", async (HttpRequest rq) =>
            {
                int statusCode = 500;

                long? userCode = null;

                var dict = await FromBody(rq);

                if (dict is not null)
                {
                    if (dict["username"] is string username && dict["password"] is string password)
                    {
                        userCode = GetCode(username, password);
                        if (users.TryGetValue(username, out long code) && code == userCode)
                        {
                            statusCode = 200;
                            return Results.Ok(new { code = userCode?.ToString("X")});
                        }else
                        {
                            statusCode = StatusCodes.Status401Unauthorized;
                        }
                    }else
                    {
                        statusCode = StatusCodes.Status400BadRequest;
                    }
                }else
                {
                    statusCode = StatusCodes.Status500InternalServerError;
                }
                return Results.Problem(statusCode: statusCode);
            });
            
            //Change Password
            app.MapPost("/passwordchange", async (HttpRequest rq) =>
            {
                var dict = await FromBody(rq);

                if (dict is not null && dict.TryGetValue("code", out object? codeObj) &&
                        dict.TryGetValue("oldpassword", out object? oldPasswdObj) &&
                        dict.TryGetValue("newpassword", out object? newPasswdObj) &&
                        dict.TryGetValue("username"   , out object? usernameObj))
                {
                    if (Convert.ToInt64(codeObj as string, 16) is long code && oldPasswdObj is string oldPasswd &&
                            newPasswdObj is string newPasswd && usernameObj is string username)
                    {
                        if (Authorize(username, oldPasswd) && Authorize(username, code))
                        {
                            users[username] = GetCode(username, newPasswd);
                            return Results.Ok(new { code = users[username].ToString("X") });
                        }
                        else return Results.Unauthorized();
                    }
                    else return Results.BadRequest("Can't parse obj to string or to int.");
                }
                else return Results.BadRequest("Values are incomplete");
            });

            //Check if user credentials are correct
            app.MapGet("/authorize", (string username, string code) =>
            {
                if (Convert.ToInt64(code, 16) is long value)
                    return Results.Ok(new { success = Authorize(username, value) });
                return Results.BadRequest("Can't convert the 'code' value to type Int64. Make sure it is a correctly formatted hexadecimal value.");
            });

            app.Run();
        }
        
        protected private static JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full
        };

        protected private static Dictionary<string, object>? FromJSON(string json)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(json, _jsonSettings);
        }

        protected private static async Task<Dictionary<string, object>?> FromBody(HttpRequest rq)
        {
            string body = "";
            using (StreamReader stream = new StreamReader(rq.Body))
            {
                body = await stream.ReadToEndAsync();
            }
            return FromJSON(body);
        }

        protected private static long GetCode(string username, string password)
        {
            long nameCode = 1;
            for (int i = 0; i < username.Length; i++)
            {
                char c = username[i];
                nameCode *= c * 50 + i * 10;
            }
            long passwordCode = 1;
            for (int i = 0; i < password.Length; i++)
            {
                char c = password[i];
                passwordCode *= c * 30 + i * 15;
            }
            return nameCode / 50 + passwordCode / 30;
        }

        protected private static bool Authorize(string username, string password)
        {
            return users.ContainsKey(username) && users[username] == GetCode(username, password);
        }

        protected private static bool Authorize(string username, long code)
        {
            return users.ContainsKey(username) && users[username] == code;
        }

        protected private static void AddDefault(params string[] users)
        {
            foreach (string user in users)
            {
                Program.users.Add(user, GetCode(user, "wachtwoord"));
            }
        }
    }
}