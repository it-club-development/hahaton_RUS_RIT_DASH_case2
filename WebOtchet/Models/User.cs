namespace WebOtchet.Models;

public class User
{
    public Guid Id {get;set;} = Guid.NewGuid();
    public string Username {get;set;} = "";
    public string Hash {get;set;} = "";
    public Roles Role {get;set;} = Roles.Engineer;
}

