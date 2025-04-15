using Microsoft.EntityFrameworkCore;

using Persistance.Data;

namespace API.Utils;

public class DeleteDatabase
{
    public static async Task Execute(IApplicationBuilder app)
    {
        //get context scope
        using var scope = app
            .ApplicationServices.GetRequiredService<IServiceScopeFactory>()
            .CreateScope();
        using var context = scope.ServiceProvider.GetService<AppDBContext>();
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        //init needed objects
        //
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }
}