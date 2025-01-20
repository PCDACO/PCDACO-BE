using Domain.Entities;

namespace UseCases.DTOs;

public class CurrentUser
{
    public User? User { get; private set; }
    public void SetUser(User user)
    {
        User = user;
    }
}