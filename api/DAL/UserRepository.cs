namespace SmwHackTracker.api.DAL;

using Dapper;
using System.Threading.Tasks;
using System.Collections.Generic;
using SmwHackTracker.api.DAL.Models;

public class UserRepository
{
    private readonly DapperContext _context;

    public UserRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<int> CreateUserAsync(User user)
    {
        var sql = "INSERT INTO Users (Username, Email, Password) VALUES (@Username, @Email, @Password)";
        using var connection = _context.CreateConnection();
        return await connection.ExecuteAsync(sql, user);
    }

    public async Task<User?> LoginAsync(string usernameOrEmail, string hashedPassword)
    {
        var sql = @"
            SELECT Id, Username, Email
            FROM Users
            WHERE (Username = @UsernameOrEmail OR Email = @UsernameOrEmail)
              AND Password = @HashedPassword
        ";
        using var connection = _context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new
        {
            UsernameOrEmail = usernameOrEmail,
            HashedPassword = hashedPassword
        });
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        var sql = @"SELECT Id, Username, Email FROM Users WHERE Id = @UserId";
        using var connection = _context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { UserId = userId });
    }
}
