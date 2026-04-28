using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class UserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        User? user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            return null;
        }

        return user;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        User? user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return null;
        }

        return user;
    }

    public async Task AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    public async Task<List<User>> GetAllAsync()
    {
        List<User> users = await _context.Users.ToListAsync();
        return users;
    }
}
