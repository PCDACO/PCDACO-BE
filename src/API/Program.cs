using API;
using API.Middlewares;
using API.Utils;

using dotenv.net;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

DotEnv.Load(options: new DotEnvOptions(probeForEnv: true));
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddServices(builder.Configuration);
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
UpdateDatabase.Execute(app);
app.UseAuthentication();
app.UseMiddleware<AuthMiddleware>();
app.UseAuthorization();
app.UseHttpsRedirection();
app.AddAppConfig();
app.Run();
