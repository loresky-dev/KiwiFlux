using RCCServiceArbiter;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

string baseUrl = Settings.BaseUrl = configuration.GetSection("BaseUrl").Value!;
string arbiterBaseUrl = configuration.GetSection("ArbiterBaseUrl").Value!;

var app = builder.Build();
Settings.BaseUrl = baseUrl;
Settings.ArbiterBaseUrl = arbiterBaseUrl;
Settings.TemplatesPath = configuration.GetSection("TemplateDirectory").Value!;
Settings.RCC2016EPath = configuration.GetSection("RCC:2016E").Value!;
Settings.RCC2018LPath = configuration.GetSection("RCC:2018L").Value!;

Console.Write($"[Startup] Running on base URL {baseUrl}, arbiter base URL: {arbiterBaseUrl}, local URL {configuration.GetSection("Urls").Value}");

app.UseAuthorization();
app.MapControllers();
app.Run();