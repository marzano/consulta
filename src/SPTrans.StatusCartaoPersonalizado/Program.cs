using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using PublicadorConciliacaoFaturamento.Domain.Services;
using Serilog;
using SPTrans.StatusCartaoPersonalizado.Configurations.Factories;
using SPTrans.StatusCartaoPersonalizado.Domain.Models.Configuration;
using SPTrans.StatusCartaoPersonalizado.Domain.Services;
using SPTrans.StatusCartaoPersonalizado.Selenium.Models;
using SPTrans.StatusCartaoPersonalizado.Selenium.Tasks;

namespace SPTrans.StatusCartaoPersonalizado
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var host = CreateHostBuilder(args).Build();

                using (host)
                {
                    Log.Logger = new LoggerConfiguration()
                        .Enrich.FromLogContext()
                        .ReadFrom.Configuration(Configuration)
                        .CreateLogger();

                    await host.StartAsync();
                    await host.WaitForShutdownAsync();
                }
            }
            finally
            {
                CreateHostBuilder(args).Build().Run();
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostContext, configuration) =>
                {
                    configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    configuration.AddEnvironmentVariables();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();
                    services.Configure<SeleniumSettings>(hostContext.Configuration.GetSection("SeleniumSettings"));
                    services.Configure<Database>(hostContext.Configuration.GetSection("Database"));
                    services.Configure<Messaging>(hostContext.Configuration.GetSection("Queue"));
                    services.Configure<ServiceData>(hostContext.Configuration.GetSection("ServiceData"));

                    services.AddSingleton<IDatabaseFactory, DatabaseFactory>();
                    services.AddSingleton<IMessagingFactory, MessagingFactory>();
                    services.AddSingleton((provider) => { return WebDriverFactory.GetWebDriver(); });

                    services.AddTransient<IMessagingService, MessagingService>();
                    services.AddTransient<ISqlService, SqlService>();

                    services.AddTransient<ICartaoFuncionarioService, CartaoFuncionarioService>();

                    services.AddTransient<ICartaoStatusSPTransTask, CartaoStatusSPTransTask>();

                    services.AddTransient<ITwoCaptchaService, TwoCaptchaService>();

                    services.AddApplicationInsightsTelemetryWorkerService();

                    services.AddHostedService<Worker>();
                })
            .UseSerilog();

        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }
}
