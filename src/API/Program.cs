using API;
using dotenv.net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

DotEnv.Load(options: new DotEnvOptions(probeForEnv: true));
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddServices(builder.Configuration);
var connectionString = builder.Configuration["ConnectionStrings"];
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.AddAppConfig();
app.Run();
