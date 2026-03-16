namespace Prevly.Application.Auth.Dtos
{
    public record AuthInfoDto(
        string Token,
        int ExpiresInMinutes,
        string AccountId,
        string Name,
        string Login
    );
}