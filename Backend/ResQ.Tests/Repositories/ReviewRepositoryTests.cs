using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Orders;
using ResQ.API.Models.Reviews;
using ResQ.API.Repositories.Reviews;

namespace ResQ.Tests.Repositories;

public class ReviewRepositoryTests : IDisposable
{
    private readonly ResQDbContext _db;
    private readonly ReviewRepository _sut;

    public ReviewRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ResQDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new ResQDbContext(options);
        _sut = new ReviewRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<ConsumerProfile> SeedConsumerAsync(string firstName = "Ana")
    {
        var consumer = new ConsumerProfile
        {
            FirstName = firstName,
            LastName  = "López",
            CreatedAt = DateTime.UtcNow
        };
        _db.ConsumerProfiles.Add(consumer);
        await _db.SaveChangesAsync();
        return consumer;
    }

    private async Task<MerchantProfile> SeedMerchantAsync(string name = "La Panadería")
    {
        var merchant = new MerchantProfile
        {
            BusinessName = name,
            Cuit         = "30-12345678-9",
            Address      = "Av. 123",
            ContactPhone = "351-000000",
            CreatedAt    = DateTime.UtcNow
        };
        _db.MerchantProfiles.Add(merchant);
        await _db.SaveChangesAsync();
        return merchant;
    }

    private async Task<Order> SeedOrderAsync(int consumerId, int merchantId, string pickupCode = "ABC123")
    {
        var order = new Order
        {
            ConsumerId        = consumerId,
            MerchantId        = merchantId,
            TotalAmount       = 500m,
            PlatformFee       = 50m,
            MerchantEarnings  = 450m,
            ExternalReference = Guid.NewGuid().ToString(),
            OrderStatus       = OrderStatus.PickedUp,
            PickupCode        = pickupCode,
            CreatedAt         = DateTime.UtcNow
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    private async Task<Review> SeedReviewAsync(
        int orderId, int merchantId, byte rating = 5, string? comment = null, DateTime? createdAt = null)
    {
        var review = new Review
        {
            OrderId    = orderId,
            MerchantId = merchantId,
            Rating     = rating,
            Comment    = comment,
            CreatedAt  = createdAt ?? DateTime.UtcNow
        };
        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();
        return review;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByMerchantIdAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByMerchantIdAsync_ReturnsOnlyReviewsForGivenMerchant()
    {
        // Arrange
        var consumer = await SeedConsumerAsync();
        var m1 = await SeedMerchantAsync("Panadería A");
        var m2 = await SeedMerchantAsync("Panadería B");
        var o1 = await SeedOrderAsync(consumer.Id, m1.Id, "CODE01");
        var o2 = await SeedOrderAsync(consumer.Id, m1.Id, "CODE02");
        var o3 = await SeedOrderAsync(consumer.Id, m2.Id, "CODE03");

        await SeedReviewAsync(o1.Id, m1.Id);
        await SeedReviewAsync(o2.Id, m1.Id);
        await SeedReviewAsync(o3.Id, m2.Id);

        // Act
        var result = (await _sut.GetByMerchantIdAsync(m1.Id)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(m1.Id, r.MerchantId));
    }

    [Fact]
    public async Task GetByMerchantIdAsync_OrdersByCreatedAtDescending()
    {
        // Arrange
        var consumer = await SeedConsumerAsync();
        var merchant = await SeedMerchantAsync();
        var now = DateTime.UtcNow;
        var oOld = await SeedOrderAsync(consumer.Id, merchant.Id, "OLD001");
        var oNew = await SeedOrderAsync(consumer.Id, merchant.Id, "NEW001");

        await SeedReviewAsync(oOld.Id, merchant.Id, comment: "Old", createdAt: now.AddDays(-2));
        await SeedReviewAsync(oNew.Id, merchant.Id, comment: "New", createdAt: now);

        // Act
        var result = (await _sut.GetByMerchantIdAsync(merchant.Id)).ToList();

        // Assert
        Assert.Equal("New", result[0].Comment);
        Assert.Equal("Old", result[1].Comment);
    }

    [Fact]
    public async Task GetByMerchantIdAsync_WhenMerchantHasNoReviews_ReturnsEmptyList()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();

        // Act
        var result = await _sut.GetByMerchantIdAsync(merchant.Id);

        // Assert
        Assert.Empty(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ExistsForOrderAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ExistsForOrderAsync_WhenReviewExistsForOrder_ReturnsTrue()
    {
        // Arrange
        var consumer = await SeedConsumerAsync();
        var merchant = await SeedMerchantAsync();
        var order    = await SeedOrderAsync(consumer.Id, merchant.Id);
        await SeedReviewAsync(order.Id, merchant.Id);

        // Act
        var result = await _sut.ExistsForOrderAsync(order.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsForOrderAsync_WhenNoReviewForOrder_ReturnsFalse()
    {
        // Arrange
        var consumer = await SeedConsumerAsync();
        var merchant = await SeedMerchantAsync();
        var order    = await SeedOrderAsync(consumer.Id, merchant.Id);

        // Act
        var result = await _sut.ExistsForOrderAsync(order.Id);

        // Assert
        Assert.False(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GenericRepository — AddAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AddAsync_ThenSaveChanges_PersistsReview()
    {
        // Arrange
        var consumer = await SeedConsumerAsync();
        var merchant = await SeedMerchantAsync();
        var order    = await SeedOrderAsync(consumer.Id, merchant.Id);
        var review = new Review
        {
            OrderId    = order.Id,
            MerchantId = merchant.Id,
            Rating     = 5,
            Comment    = "Excelente",
            CreatedAt  = DateTime.UtcNow
        };

        // Act
        await _sut.AddAsync(review);
        await _sut.SaveChangesAsync();

        // Assert
        var saved = await _db.Reviews.FindAsync(review.Id);
        Assert.NotNull(saved);
        Assert.Equal("Excelente", saved.Comment);
    }
}
