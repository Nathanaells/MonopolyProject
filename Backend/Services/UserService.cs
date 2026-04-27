using Backend.Domain.Entities;
using Backend.DTOs;

public class UserService
{
    private readonly UserRepository _userRepository;

    public UserService(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User?> LoginAsync(UserLoginDTO loginDTO)
    {
        string username = loginDTO.Username;
        string password = loginDTO.Password;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        User? user = await _userRepository.GetByUsernameAsync(username);

        if (user == null || user.Password != password)
        {
            return null;
        }

        return user;
    }

    public async Task<User?> RegisterAsync(UserRegisterDTO registerDTO)
    {
        string name = registerDTO.Name;
        string username = registerDTO.Username;
        string password = registerDTO.Password;

        if (
            string.IsNullOrEmpty(name)
            || string.IsNullOrEmpty(username)
            || string.IsNullOrEmpty(password)
        )
        {
            return null;
        }

        User? existingUser = await _userRepository.GetByUsernameAsync(username);

        if (existingUser != null)
        {
            return null;
        }

        User newUser = new User(name, username, password);
        await _userRepository.AddAsync(newUser);

        return newUser;
    }
}
