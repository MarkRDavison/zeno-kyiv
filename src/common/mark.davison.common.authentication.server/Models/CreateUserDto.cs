namespace mark.davison.common.authentication.server.Models;

public record CreateUserDto(UserDto User, IReadOnlyList<string> Roles);
