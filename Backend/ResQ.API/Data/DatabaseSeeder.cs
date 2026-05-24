using ResQ.API.Models.Auth;
using ResQ.API.Models.Catalog;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Orders;
using ResQ.API.Models.Reviews;

namespace ResQ.API.Data;

/// <summary>
/// Seeds demo data for local development. Runs once on startup if the DB is empty.
/// Credentials for all seeded accounts: ResQ1234!
/// </summary>
public static class DatabaseSeeder
{
    public static void Seed(ResQDbContext db)
    {
        // Idempotent guard — skip if already seeded
        if (db.Categories.Any()) return;

        var now = DateTime.UtcNow;

        // BCrypt work factor 8 is intentionally lower than production (12) to keep seeding fast
        var hash = BCrypt.Net.BCrypt.HashPassword("ResQ1234!", 8);

        // ─── 1. Categories ────────────────────────────────────────────────────────
        var catPanaderia   = new Category { Name = "Panadería",   CreatedAt = now };
        var catSushi       = new Category { Name = "Sushi",       CreatedAt = now };
        var catCafe        = new Category { Name = "Café",        CreatedAt = now };
        var catRestaurante = new Category { Name = "Restaurante", CreatedAt = now };
        var catVegano      = new Category { Name = "Vegano",      CreatedAt = now };
        var catHeladeria   = new Category { Name = "Heladería",   CreatedAt = now };

        db.Categories.AddRange(catPanaderia, catSushi, catCafe, catRestaurante, catVegano, catHeladeria);
        db.SaveChanges();

        // ─── 2. Users with profiles and roles ────────────────────────────────────
        var consumerUser = new User
        {
            Email        = "consumer@resq.com",
            PasswordHash = hash,
            IsActive     = true,
            CreatedAt    = now,
            ConsumerProfile = new ConsumerProfile
            {
                FirstName   = "Ignacio",
                LastName    = "Grudine",
                PhoneNumber = "+54 351 555-0001",
                CreatedAt   = now
            },
            UserRoles = [new UserRole { Role = Role.Consumer, CreatedAt = now }]
        };

        var panaderiaUser = new User
        {
            Email        = "panaderia@resq.com",
            PasswordHash = hash,
            IsActive     = true,
            CreatedAt    = now,
            MerchantProfile = new MerchantProfile
            {
                BusinessName       = "La Panadería del Centro",
                Cuit               = "30-12345678-0",
                Address            = "Obispo Trejo 220, Córdoba",
                Latitude           = -31.4153m,
                Longitude          = -64.1869m,
                ContactPhone       = "+54 351 555-1001",
                MpConnectionStatus = MpConnectionStatus.Disconnected,
                CreatedAt          = now
            },
            UserRoles = [new UserRole { Role = Role.Merchant, CreatedAt = now }]
        };

        var sushiUser = new User
        {
            Email        = "sushi@resq.com",
            PasswordHash = hash,
            IsActive     = true,
            CreatedAt    = now,
            MerchantProfile = new MerchantProfile
            {
                BusinessName       = "Sushi Nakamura",
                Cuit               = "30-23456789-1",
                Address            = "Av. Hipólito Yrigoyen 410, Nueva Córdoba",
                Latitude           = -31.4289m,
                Longitude          = -64.1870m,
                ContactPhone       = "+54 351 555-2002",
                MpConnectionStatus = MpConnectionStatus.Disconnected,
                CreatedAt          = now
            },
            UserRoles = [new UserRole { Role = Role.Merchant, CreatedAt = now }]
        };

        var cafeUser = new User
        {
            Email        = "cafe@resq.com",
            PasswordHash = hash,
            IsActive     = true,
            CreatedAt    = now,
            MerchantProfile = new MerchantProfile
            {
                BusinessName       = "Café Postal",
                Cuit               = "30-34567890-2",
                Address            = "27 de Abril 190, Córdoba Centro",
                Latitude           = -31.4102m,
                Longitude          = -64.1874m,
                ContactPhone       = "+54 351 555-3003",
                MpConnectionStatus = MpConnectionStatus.Disconnected,
                CreatedAt          = now
            },
            UserRoles = [new UserRole { Role = Role.Merchant, CreatedAt = now }]
        };

        db.Users.AddRange(consumerUser, panaderiaUser, sushiUser, cafeUser);
        db.SaveChanges();

        var consumer = consumerUser.ConsumerProfile!;
        var panaderia = panaderiaUser.MerchantProfile!;
        var sushi     = sushiUser.MerchantProfile!;
        var cafe      = cafeUser.MerchantProfile!;

        // ─── 3. Merchant ↔ Category links ────────────────────────────────────────
        db.MerchantCategories.AddRange(
            new MerchantCategory { MerchantId = panaderia.Id, CategoryId = catPanaderia.Id },
            new MerchantCategory { MerchantId = sushi.Id,     CategoryId = catSushi.Id     },
            new MerchantCategory { MerchantId = cafe.Id,      CategoryId = catCafe.Id      }
        );
        db.SaveChanges();

        // ─── 4. Products (packs) ─────────────────────────────────────────────────
        var packSorpresaDulce = new Product
        {
            MerchantId      = panaderia.Id,
            Name            = "Pack Sorpresa Dulce",
            Description     = "Selección sorpresa de facturas, medialunas y tortas finas del día. ¡Siempre algo distinto!",
            ProductType     = ProductType.SurprisePack,
            OriginalPrice   = 1500m,
            SalePrice       = 700m,
            StockQuantity   = 5,
            PickupTimeStart = new TimeOnly(17, 0),
            PickupTimeEnd   = new TimeOnly(20, 0),
            IsActive        = true,
            CreatedAt       = now
        };

        var packMedialunas = new Product
        {
            MerchantId      = panaderia.Id,
            Name            = "Pack Medialunas del Día",
            Description     = "12 medialunas de manteca del horno de la tarde. Perfectas para acompañar con mate.",
            ProductType     = ProductType.ExplicitItem,
            OriginalPrice   = 800m,
            SalePrice       = 400m,
            StockQuantity   = 8,
            PickupTimeStart = new TimeOnly(18, 0),
            PickupTimeEnd   = new TimeOnly(21, 0),
            IsActive        = true,
            CreatedAt       = now
        };

        var packMixtoSushi = new Product
        {
            MerchantId      = sushi.Id,
            Name            = "Pack Mixto Sushi",
            Description     = "30 piezas de sushi variado del día: rolls, nigiris y temakis frescos. La sorpresa de cada jornada.",
            ProductType     = ProductType.SurprisePack,
            OriginalPrice   = 3500m,
            SalePrice       = 1800m,
            StockQuantity   = 3,
            PickupTimeStart = new TimeOnly(20, 0),
            PickupTimeEnd   = new TimeOnly(22, 0),
            IsActive        = true,
            CreatedAt       = now
        };

        var packRolls = new Product
        {
            MerchantId      = sushi.Id,
            Name            = "Pack Rolls Variados",
            Description     = "16 piezas de rolls seleccionados: Philadelphia, California, Spicy Tuna y más.",
            ProductType     = ProductType.ExplicitItem,
            OriginalPrice   = 2800m,
            SalePrice       = 1400m,
            StockQuantity   = 5,
            PickupTimeStart = new TimeOnly(19, 30),
            PickupTimeEnd   = new TimeOnly(22, 30),
            IsActive        = true,
            CreatedAt       = now
        };

        var packCafe = new Product
        {
            MerchantId      = cafe.Id,
            Name            = "Pack Café & Medialunas",
            Description     = "Café de especialidad + 4 medialunas de manteca. El desayuno o merienda perfecta.",
            ProductType     = ProductType.SurprisePack,
            OriginalPrice   = 900m,
            SalePrice       = 450m,
            StockQuantity   = 10,
            PickupTimeStart = new TimeOnly(16, 0),
            PickupTimeEnd   = new TimeOnly(19, 0),
            IsActive        = true,
            CreatedAt       = now
        };

        var packMerienda = new Product
        {
            MerchantId      = cafe.Id,
            Name            = "Pack Merienda Completa",
            Description     = "Taza de té o café + 2 tostadas con mermelada artesanal + porción de torta del día.",
            ProductType     = ProductType.ExplicitItem,
            OriginalPrice   = 1200m,
            SalePrice       = 600m,
            StockQuantity   = 7,
            PickupTimeStart = new TimeOnly(15, 0),
            PickupTimeEnd   = new TimeOnly(18, 0),
            IsActive        = true,
            CreatedAt       = now
        };

        db.Products.AddRange(packSorpresaDulce, packMedialunas, packMixtoSushi, packRolls, packCafe, packMerienda);
        db.SaveChanges();

        // ─── 5. Orders ───────────────────────────────────────────────────────────
        // Current orders (shown in Mis Órdenes)
        var order1 = new Order  // Paid — consumer can see pickup code
        {
            ConsumerId        = consumer.Id,
            MerchantId        = panaderia.Id,
            TotalAmount       = 700m,
            PlatformFee       = 70m,
            MerchantEarnings  = 630m,
            ExternalReference = Guid.NewGuid().ToString(),
            OrderStatus       = OrderStatus.Paid,
            PickupCode        = "RSQ-4872",
            CreatedAt         = now
        };
        var order2 = new Order  // PickedUp
        {
            ConsumerId        = consumer.Id,
            MerchantId        = sushi.Id,
            TotalAmount       = 1800m,
            PlatformFee       = 180m,
            MerchantEarnings  = 1620m,
            ExternalReference = Guid.NewGuid().ToString(),
            OrderStatus       = OrderStatus.PickedUp,
            PickupCode        = "RSQ-3341",
            CreatedAt         = now.AddDays(-1)
        };
        var order3 = new Order  // Cancelled
        {
            ConsumerId        = consumer.Id,
            MerchantId        = cafe.Id,
            TotalAmount       = 450m,
            PlatformFee       = 0m,
            MerchantEarnings  = 0m,
            ExternalReference = Guid.NewGuid().ToString(),
            OrderStatus       = OrderStatus.Cancelled,
            PickupCode        = "RSQ-9921",
            CreatedAt         = now.AddDays(-2)
        };

        // Historical orders — completed, generate reviews per merchant
        var orderH1 = new Order { ConsumerId = consumer.Id, MerchantId = sushi.Id,     TotalAmount = 1400m, PlatformFee = 140m, MerchantEarnings = 1260m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-1001", CreatedAt = now.AddDays(-7)  };
        var orderH2 = new Order { ConsumerId = consumer.Id, MerchantId = sushi.Id,     TotalAmount = 1800m, PlatformFee = 180m, MerchantEarnings = 1620m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-1002", CreatedAt = now.AddDays(-14) };
        var orderH3 = new Order { ConsumerId = consumer.Id, MerchantId = panaderia.Id, TotalAmount = 700m,  PlatformFee = 70m,  MerchantEarnings = 630m,  ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-1003", CreatedAt = now.AddDays(-10) };
        var orderH4 = new Order { ConsumerId = consumer.Id, MerchantId = panaderia.Id, TotalAmount = 400m,  PlatformFee = 40m,  MerchantEarnings = 360m,  ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-1004", CreatedAt = now.AddDays(-17) };
        var orderH5 = new Order { ConsumerId = consumer.Id, MerchantId = panaderia.Id, TotalAmount = 700m,  PlatformFee = 70m,  MerchantEarnings = 630m,  ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-1005", CreatedAt = now.AddDays(-24) };
        var orderH6 = new Order { ConsumerId = consumer.Id, MerchantId = cafe.Id,      TotalAmount = 600m,  PlatformFee = 60m,  MerchantEarnings = 540m,  ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-1006", CreatedAt = now.AddDays(-5)  };
        var orderH7 = new Order { ConsumerId = consumer.Id, MerchantId = cafe.Id,      TotalAmount = 450m,  PlatformFee = 45m,  MerchantEarnings = 405m,  ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-1007", CreatedAt = now.AddDays(-12) };

        db.Orders.AddRange(order1, order2, order3, orderH1, orderH2, orderH3, orderH4, orderH5, orderH6, orderH7);
        db.SaveChanges();

        // ─── 6. Order details ─────────────────────────────────────────────────────
        db.OrderDetails.AddRange(
            new OrderDetail { OrderId = order1.Id,  ProductId = packSorpresaDulce.Id, Quantity = 1, UnitPrice = 700m,  CreatedAt = now            },
            new OrderDetail { OrderId = order2.Id,  ProductId = packMixtoSushi.Id,    Quantity = 1, UnitPrice = 1800m, CreatedAt = now.AddDays(-1) },
            new OrderDetail { OrderId = order3.Id,  ProductId = packCafe.Id,          Quantity = 1, UnitPrice = 450m,  CreatedAt = now.AddDays(-2) },
            new OrderDetail { OrderId = orderH1.Id, ProductId = packRolls.Id,         Quantity = 1, UnitPrice = 1400m, CreatedAt = now.AddDays(-7)  },
            new OrderDetail { OrderId = orderH2.Id, ProductId = packMixtoSushi.Id,    Quantity = 1, UnitPrice = 1800m, CreatedAt = now.AddDays(-14) },
            new OrderDetail { OrderId = orderH3.Id, ProductId = packSorpresaDulce.Id, Quantity = 1, UnitPrice = 700m,  CreatedAt = now.AddDays(-10) },
            new OrderDetail { OrderId = orderH4.Id, ProductId = packMedialunas.Id,    Quantity = 1, UnitPrice = 400m,  CreatedAt = now.AddDays(-17) },
            new OrderDetail { OrderId = orderH5.Id, ProductId = packSorpresaDulce.Id, Quantity = 1, UnitPrice = 700m,  CreatedAt = now.AddDays(-24) },
            new OrderDetail { OrderId = orderH6.Id, ProductId = packMerienda.Id,      Quantity = 1, UnitPrice = 600m,  CreatedAt = now.AddDays(-5)  },
            new OrderDetail { OrderId = orderH7.Id, ProductId = packCafe.Id,          Quantity = 1, UnitPrice = 450m,  CreatedAt = now.AddDays(-12) }
        );
        db.SaveChanges();

        // ─── 7. Reviews ──────────────────────────────────────────────────────────
        // Sushi Nakamura: 3 reviews → avg 4.7
        // La Panadería del Centro: 3 reviews → avg 4.3
        // Café Postal: 2 reviews → avg 4.5
        db.Reviews.AddRange(
            new Review { OrderId = order2.Id,  MerchantId = sushi.Id,     Rating = 5, Comment = "El sushi estaba increíble, muy fresco y abundante. 100% recomendado!",          CreatedAt = now.AddDays(-1)  },
            new Review { OrderId = orderH1.Id, MerchantId = sushi.Id,     Rating = 5, Comment = "Excelente presentación y frescura. Los rolls son espectaculares.",              CreatedAt = now.AddDays(-7)  },
            new Review { OrderId = orderH2.Id, MerchantId = sushi.Id,     Rating = 4, Comment = "Muy buen sushi, variado y fresco. El precio es una ganga total.",               CreatedAt = now.AddDays(-14) },
            new Review { OrderId = orderH3.Id, MerchantId = panaderia.Id, Rating = 5, Comment = "Pack sorpresa espectacular, muy variado y todo recién horneado. Ahorro enorme!", CreatedAt = now.AddDays(-10) },
            new Review { OrderId = orderH4.Id, MerchantId = panaderia.Id, Rating = 4, Comment = "Muy buenas medialunas, bastante frescas. El precio es excelente.",              CreatedAt = now.AddDays(-17) },
            new Review { OrderId = orderH5.Id, MerchantId = panaderia.Id, Rating = 4, Comment = "Buena relación precio/calidad. Las facturas del día estaban muy ricas.",        CreatedAt = now.AddDays(-24) },
            new Review { OrderId = orderH6.Id, MerchantId = cafe.Id,      Rating = 5, Comment = "Café de primera y tostadas riquísimas. Los volvería a pedir sin dudas.",        CreatedAt = now.AddDays(-5)  },
            new Review { OrderId = orderH7.Id, MerchantId = cafe.Id,      Rating = 4, Comment = "Muy rico y abundante para la merienda. El café es excelente.",                  CreatedAt = now.AddDays(-12) }
        );
        db.SaveChanges();
    }
}
