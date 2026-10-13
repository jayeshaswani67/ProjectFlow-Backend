using ProjectFlow.Api.DTOs;

namespace ProjectFlow.Api.Interfaces;

public interface IUserService
{
    Task<ProfileDto?> GetProfileAsync(string userId);

    Task<ProfileDto?> UpdateProfileAsync(
        string userId,
        string name
    );
}
