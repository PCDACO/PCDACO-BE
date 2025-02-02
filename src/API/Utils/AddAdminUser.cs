using Ardalis.Result;

using MediatR;

using UseCases.UC_User.Commands;

namespace API.Utils;

public class AddAdminUser
{
    public static async Task Execute(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices
        .GetRequiredService<IServiceScopeFactory>()
        .CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        Result result = await sender.Send(new CreateAdminUser.Command());
        if (!result.IsSuccess) throw new Exception("Create Admin Failed !");
    }
}