using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.ValueObjects;
using MinimalAPI.Domain.Enums;
using System.Linq;

namespace MinimalAPI.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Kiểm tra nếu đã có data thì không seed
        if (context.Categories.Any() || context.Products.Any() || context.Customers.Any())
        {
            return;
        }

        // Tạo Categories
        var electronics = Category.Create("Điện tử", "Các sản phẩm điện tử");
        var clothing = Category.Create("Thời trang", "Quần áo, giày dép");
        var books = Category.Create("Sách", "Sách và tài liệu");

        context.Categories.AddRange(electronics, clothing, books);
        await context.SaveChangesAsync();

        // Tạo Products
        var products = new[]
        {
            Product.Create(
                "SP00001",
                ProductName.Create("Laptop Dell XPS 13"),
                Money.VND(25000000),
                electronics.Id,
                "Laptop cao cấp, màn hình 13 inch"
            ),
            Product.Create(
                "SP00002",
                ProductName.Create("iPhone 15 Pro"),
                Money.VND(30000000),
                electronics.Id,
                "Điện thoại thông minh Apple mới nhất"
            ),
            Product.Create(
                "SP00003",
                ProductName.Create("Áo thun nam"),
                Money.VND(150000),
                clothing.Id,
                "Áo thun cotton 100%"
            ),
            Product.Create(
                "SP00004",
                ProductName.Create("Giày thể thao Nike"),
                Money.VND(2500000),
                clothing.Id,
                "Giày chạy bộ chuyên nghiệp"
            ),
            Product.Create(
                "SP00005",
                ProductName.Create("Clean Code"),
                Money.VND(350000),
                books.Id,
                "Sách lập trình - Robert C. Martin"
            )
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        // Tạo Inventories tương ứng cho từng sản phẩm
        var inventories = products.Select(p => Inventory.Create(p.Id, 100)).ToArray();
        context.Inventories.AddRange(inventories);

        // Tạo Customers mẫu
        var customer1 = Customer.Create(
            "KH00001",
            "Nguyễn Văn A",
            "nguyenvana@gmail.com",
            "0901234567",
            CustomerType.Individual,
            null,
            Gender.Male,
            new DateTime(1990, 1, 1),
            "123 Đường Lê Lợi, TP. HCM",
            "Khách hàng VIP"
        );
        var customer2 = Customer.Create(
            "KH00002",
            "Trần Thị B",
            "tranthib@gmail.com",
            "0987654321",
            CustomerType.Individual,
            null,
            Gender.Female,
            new DateTime(1995, 5, 5),
            "456 Đường Nguyễn Huệ, TP. HCM",
            null
        );
        context.Customers.AddRange(customer1, customer2);

        // Khởi tạo các bộ đếm mã tự sinh (CodeCounters)
        var spCounter = new CodeCounter { Prefix = "SP", CurrentValue = 5 };
        var khCounter = new CodeCounter { Prefix = "KH", CurrentValue = 2 };
        var dhCounter = new CodeCounter { Prefix = "DH", CurrentValue = 0 };

        context.CodeCounters.AddRange(spCounter, khCounter, dhCounter);
        
        await context.SaveChangesAsync();   
    }
}
