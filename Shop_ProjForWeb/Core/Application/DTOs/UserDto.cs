namespace Shop_ProjForWeb.Core.Application.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public bool IsVip { get; set; }
    public DateTime CreatedAt { get; set; }
}
