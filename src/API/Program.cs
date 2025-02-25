using API;
using API.Middlewares;
using API.Utils;
using CloudinaryDotNet;
using dotenv.net;

using Hangfire;

using UseCases.Services.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

DotEnv.Load(options: new DotEnvOptions(probeForEnv: true));
builder.Configuration.AddEnvironmentVariables();
Cloudinary cloudinary = new Cloudinary(Environment.GetEnvironmentVariable("CLOUDINARY_URL"));
cloudinary.Api.Secure = true;

builder.Services.AddSingleton(cloudinary);
builder.Services.AddServices(builder.Configuration);
builder.Services.AddPayOSService(builder.Configuration);
builder.Services.AddEmailService(builder.Configuration);
builder.Services.AddSignalR();
builder.Services.AddHangFireService(builder.Configuration);

var app = builder.Build();

app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MyAPI");
        c.InjectStylesheet("/swagger-ui/SwaggerDark.css");
    });
}
// await UpdateDatabase.Execute(app);
// await AddAdminUser.Execute(app);
app.UseAuthentication();
app.UseMiddleware<AuthMiddleware>();
app.UseAuthorization();
app.UseHttpsRedirection();

app.MapHub<LocationHub>("location-hub");

app.UseHangfireDashboard();

app.AddAppConfig();
app.Run();
