using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WebApi.Code;

public class UsersService
{
    private static readonly object Lock = new object();

    private readonly IConfiguration configuration;

    public UsersService(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public async Task<string> LoginAsync(string login, string password)
    {
        await Task.CompletedTask;

        UserDto user = null;
        var adminUser = new UserDto()
        {
            Login = "admin",
            Roles = UserRoles.Admin,
            Password = configuration.GetValue<string>("AdminPwd")
        };
        var regularUser = new UserDto()
        {
            Login = "user",
            Roles = UserRoles.RegularUser,
            Password = configuration.GetValue<string>("UserPwd")
        };
        if (login == adminUser.Login && password == adminUser.Password)
        {
            user = adminUser;
        }
        else if (login == regularUser.Login && password == regularUser.Password)
        {
            user = regularUser;
        }

        if (user != null)
        {
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Login),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            foreach (var userRole in user.Roles.FromFlagsToList())
            {
                if (userRole == UserRoles.None)
                {
                    continue;
                }

                authClaims.Add(new Claim(ClaimTypes.Role, Enum.GetName(typeof(UserRoles), userRole).ToLowerInvariant(),
                    ClaimValueTypes.String));
            }

            var token = JwtTokenHelper.MakeToken(authClaims, configuration);

            return token;
        }

        return string.Empty;
    }

    public async Task<List<User>> GetAllAsync()
    {
        var users = await dbContext.Users.ToListAsync();
        return users;
    }

    public async Task<User> GetByIdAsync(long id)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id);
        return user;
    }

    public async Task<User> GetByLoginAsync(string login)
    {
        var users = await dbContext.Users.ToListAsync();
        var user = users.FirstOrDefault(u => u.Login.ToLowerInvariant() == login.ToLowerInvariant());
        return user;
    }

    public long Create(User user)
    {
        lock (Lock)
        {
            if (dbContext.Users.ToList()
                .Any(u => u.Login.ToLowerInvariant() == user.Login.ToLowerInvariant()))
            {
                //user with the same login exist
                return (long)ErrorCodes.EntityAlreadyExists;
            }

            string salt = string.Empty;
            do
            {
                salt = CryptographyHelper.CreateSalt(8);
            } while (dbContext.Users.Any(u => u.PasswordSalt == salt));

            var passwordHash = CryptographyHelper.GenerateHash(user.Password, salt);

            user.PasswordSalt = salt;
            user.PasswordHash = passwordHash;

            dbContext.Users.Add(user);
            dbContext.SaveChanges();

            return user.Id;
        }
        return (long)ErrorCodes.EntityNotFound;
    }

    public async Task<long> UpdateAsync(User user)
    {
        var existingUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        if (existingUser == null)
        {
            // not found
            return (long)ErrorCodes.EntityNotFound;
        }

        if (!string.IsNullOrEmpty(user.Password))
        {
            var passwordHash = CryptographyHelper.GenerateHash(user.Password, existingUser.PasswordSalt);
            existingUser.PasswordHash = passwordHash;
        }

        await dbContext.SaveChangesAsync();

        return user.Id;
    }

    public async Task<long> DeleteAsync(User user)
    {
        var existingUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        if (existingUser == null)
        {
            // not found
            return (long)ErrorCodes.EntityNotFound;
        }

        dbContext.Users.Remove(existingUser);
        await dbContext.SaveChangesAsync();

        return user.Id;
    }
}