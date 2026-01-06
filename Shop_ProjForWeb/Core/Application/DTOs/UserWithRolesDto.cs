namespace Shop_ProjForWeb.Core.Application.DTOs;

public class UserWithRolesDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public IEnumerable<string> Roles { get; set; } = new List<string>();
}
