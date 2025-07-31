using SmwHackTracker.api.DAL.Models;
using Dapper;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace SmwHackTracker.api.DAL
{
    public class PasswordRepository
    {
        private readonly DapperContext _context;

        public PasswordRepository(DapperContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Create a new password entry
        /// </summary>
        public async Task<Guid> CreatePasswordAsync(Password password)
        {
            password.Id = Guid.NewGuid();
            password.CreatedAt = DateTime.UtcNow;
            password.UpdatedAt = DateTime.UtcNow;

            var sql = @"
                INSERT INTO Passwords (Id, Platform, Username, EncryptedPassword, Comment, UserId, CreatedAt, UpdatedAt) 
                VALUES (@Id, @Platform, @Username, @EncryptedPassword, @Comment, @UserId, @CreatedAt, @UpdatedAt)";
            
            using var connection = _context.CreateConnection();
            await connection.ExecuteAsync(sql, password);
            return password.Id;
        }

        /// <summary>
        /// Get all password entries for a specific user
        /// </summary>
        public async Task<IEnumerable<Password>> GetAllPasswordsAsync(Guid userId)
        {
            var sql = @"
                SELECT Id, Platform, Username, EncryptedPassword, Comment, UserId, CreatedAt, UpdatedAt 
                FROM Passwords 
                WHERE UserId = @UserId 
                ORDER BY Platform, Username";
            
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<Password>(sql, new { UserId = userId });
        }

        /// <summary>
        /// Get a specific password entry by ID
        /// </summary>
        public async Task<Password?> GetPasswordByIdAsync(Guid passwordId, Guid userId)
        {
            var sql = @"
                SELECT Id, Platform, Username, EncryptedPassword, Comment, UserId, CreatedAt, UpdatedAt 
                FROM Passwords 
                WHERE Id = @PasswordId AND UserId = @UserId";
            
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Password>(sql, new { PasswordId = passwordId, UserId = userId });
        }

        /// <summary>
        /// Update an existing password entry
        /// </summary>
        public async Task<bool> UpdatePasswordAsync(Password password)
        {
            password.UpdatedAt = DateTime.UtcNow;

            var sql = @"
                UPDATE Passwords 
                SET Platform = @Platform, Username = @Username, EncryptedPassword = @EncryptedPassword, 
                    Comment = @Comment, UpdatedAt = @UpdatedAt 
                WHERE Id = @Id AND UserId = @UserId";
            
            using var connection = _context.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, password);
            return rowsAffected > 0;
        }

        /// <summary>
        /// Delete a password entry
        /// </summary>
        public async Task<bool> DeletePasswordAsync(Guid passwordId, Guid userId)
        {
            var sql = @"DELETE FROM Passwords WHERE Id = @PasswordId AND UserId = @UserId";
            
            using var connection = _context.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { PasswordId = passwordId, UserId = userId });
            return rowsAffected > 0;
        }

        /// <summary>
        /// Search password entries by platform or username
        /// </summary>
        public async Task<IEnumerable<Password>> SearchPasswordsAsync(Guid userId, string searchTerm)
        {
            var sql = @"
                SELECT Id, Platform, Username, EncryptedPassword, Comment, UserId, CreatedAt, UpdatedAt 
                FROM Passwords 
                WHERE UserId = @UserId 
                AND (Platform LIKE @SearchTerm OR Username LIKE @SearchTerm OR Comment LIKE @SearchTerm)
                ORDER BY Platform, Username";
            
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<Password>(sql, new { 
                UserId = userId, 
                SearchTerm = $"%{searchTerm}%" 
            });
        }

        /// <summary>
        /// Get password count for a user
        /// </summary>
        public async Task<int> GetPasswordCountAsync(Guid userId)
        {
            var sql = @"SELECT COUNT(*) FROM Passwords WHERE UserId = @UserId";
            
            using var connection = _context.CreateConnection();
            return await connection.QuerySingleAsync<int>(sql, new { UserId = userId });
        }
    }
}