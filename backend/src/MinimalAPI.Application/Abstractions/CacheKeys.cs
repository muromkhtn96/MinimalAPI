public static class CacheKeys
{
    // Product
    public static string ProductById(Guid id) => $"product:id:{id}";
    public static string ProductByCode(string code) => $"product:code:{code}";

    // Customer
    public static string CustomerById(Guid id) => $"customer:id:{id}";
    public static string CustomerByCode(string code) => $"customer:code:{code}";

    // Category
    public static string CategoryById(Guid id) => $"category:id:{id}";

    // Order
    public static string OrderById(Guid id) => $"order:id:{id}";
    public static string OrderByCode(string code) => $"order:code:{code}";
}