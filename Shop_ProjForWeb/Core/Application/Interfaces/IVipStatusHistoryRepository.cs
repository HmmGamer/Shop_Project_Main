namespace Shop_ProjForWeb.Core.Application.Interfaces;

using Shop_ProjForWeb.Core.Domain.Entities;

public interface IVipStatusHistoryRepository
{
    Task<VipStatusHistory?> GetByIdAsync(Guid id);
    Task<List<VipStatusHistory>> GetAllAsync();
    Task<List<VipStatusHistory>> GetByUserIdAsync(Guid userId);
    Task AddAsync(VipStatusHistory vipStatusHistory);
    Task UpdateAsync(VipStatusHistory vipStatusHistory);
    Task DeleteAsync(Guid id);
}