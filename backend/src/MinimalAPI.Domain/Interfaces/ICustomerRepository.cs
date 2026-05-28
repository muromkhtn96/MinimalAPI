using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Domain.Interfaces;

public interface ICustomerRepository : IRepository<Customer, CustomerId>
{
   /// <summary>
   /// Lấy danh sách khách hàng với phân trang và bộ lọc.
   /// </summary>
   /// <param name="keyword"></param>
   /// <param name="page"></param>
   /// <param name="pageSize"></param>
   /// <param name="ct"></param>
   /// <returns></returns> <summary>
   Task<List<Customer>> GetAllAsync(string? keyword, int page, int pageSize, CancellationToken ct = default);
   /// <summary>
   /// Lấy thông tin khách hàng theo ID.
   /// </summary>
   /// <param name="id"></param>
   /// <param name="ct"></param>
   /// <returns></returns>
   Task<bool> ExistsByPhoneAsync(string phone, CancellationToken ct = default);
   /// <summary>
   /// Lấy thông tin khách hàng theo mã.
   /// </summary>
   /// <param name="code"></param>
   /// <param name="ct"></param>
   /// <returns></returns>
   Task<Customer?> GetByCodeAsync(string code, CancellationToken ct = default);
}
