using Luval.AuthMate.Core;
using Luval.AuthMate.Infrastructure.Configuration;
using Luval.AuthMate.Infrastructure.Data;
using Luval.AuthMate.Infrastructure.Logging;
using Luval.AuthMate.Sqlite;
using Luval.GenAIBotMate.Infrastructure.Configuration;
using Luval.GenAIBotMate.Infrastructure.Data;
using Luval.WorkMate.Infrastructure.Configuration;
using Luval.WorkMate.UI.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.IdentityModel.Tokens;

namespace Luval.WorkMate.UI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var config = builder.Configuration;
            var connString = "Data Source=app.db";

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            builder.Services.AddFluentUIComponents();

            //Add logging services is a required dependency for AuthMate
            builder.Services.AddLogging();

            //Add the controllers and the http client and context accessor
            builder.Services.AddControllers();
            builder.Services.AddHttpClient();
            builder.Services.AddHttpContextAccessor();

            //Add the AuthMate services
            builder.Services.AddAuthMateServices(
                //The key to use for the bearing token implementation
                config["AuthMate:BearingTokenKey"],
                (s) => {
                    //returns a local instance of Sqlite
                    //replace this with your own implementation of Postgres, MySql, SqlServer, etc
                    return new SqliteAuthMateContext(connString);
                });

            //Add the AuthMate Google OAuth provider
            builder.Services.AddAuthMateGoogleAuth(new GoogleOAuthConfiguration()
            {
                // client id from your config file
                ClientId = config["OAuthProviders:Google:ClientId"] ?? throw new ArgumentNullException("The Google client id is required"),
                // the client secret from your config file
                ClientSecret = config["OAuthProviders:Google:ClientSecret"] ?? throw new ArgumentNullException("The Google client secret is required"),
                // set the login path in the controller and pass the provider name
                LoginPath = "/api/auth",
            });

            builder.Services.AddGenAIBotServicesDefault(
                config.GetValue<string>("OpenAIKey"),
                config.GetValue<string>("Azure:Storage:ConnectionString"),
                connString
            );

            //Add the WorkMate services
            builder.Services.AddWorkMateServices();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            //Add the authentication and authorization
            app.UseAuthentication();
            app.UseAuthorization();

            //Map the controllers and the razor components
            app.MapControllers();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            //Inialize the app database using Sqlite
            var contextHelper = new AuthMateContextHelper(
                new SqliteAuthMateContext(connString),
                new ColorConsoleLogger<AuthMateContextHelper>());
            //Makes sure the db is created, then initializes the db with the owner email
            //and required initial records
            contextHelper.InitializeDbAsync(config["OAuthProviders:Google:OwnerEmail"] ?? "someone@gmail.com")
                .GetAwaiter()
                .GetResult();

            // Initialize the database
            var genContextHelp = new GenAIBotContextHelper(new SqliteChatDbContext(connString), new ColorConsoleLogger<GenAIBotContextHelper>());
            genContextHelp.InitializeAsync()
                .GetAwaiter()
                .GetResult();

            app.Run();
        }
    }
}
