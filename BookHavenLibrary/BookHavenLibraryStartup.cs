using BookHavenLibrary.Repositories;
using BookHavenLibrary.Repositories.Interfaces;
using BookHavenLibrary.Services;

public static class BookHavenLibraryStartup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        //services.AddTransient<ITokenService, TokenService>();
        services.AddTransient<IBookRepository, BookRepository>();
        services.AddTransient<IBookmarkRepository, BookmarkRepository>();
        services.AddTransient<IAnnouncementRepository, AnnouncementRepository>();
        services.AddTransient<ICartRepository, CartRepository>();
        services.AddTransient<IDiscountRepository, DiscountRepository>();
        services.AddTransient<IReviewRepository, ReviewRepository>();
        services.AddTransient<IPurchaseRepository, PurchaseRepository>();
        services.AddTransient<IInventoryRepository, InventoryRepository>();
        services.AddTransient<ICategoryRepository, CategoryRepository>();
    }
}

