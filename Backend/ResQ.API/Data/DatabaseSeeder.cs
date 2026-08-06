using Microsoft.EntityFrameworkCore;
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
    /// <summary>
    /// Idempotently ensures a platform administrator account exists. Unlike <see cref="Seed"/>
    /// (which only runs on an empty database), this runs on every startup so the admin account
    /// is provisioned even on databases that were seeded before the admin module existed.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="email">Admin login email (from configuration, with a dev default).</param>
    /// <param name="password">Admin plain-text password to hash (from configuration, with a dev default).</param>
    /// <remarks>
    /// The admin has no consumer or merchant profile — only the <see cref="Role.Admin"/> role.
    /// There is no public registration path that grants this role.
    /// </remarks>
    public static void EnsureAdmin(ResQDbContext db, string email, string password)
    {
        email = email.ToLower().Trim();

        var adminExists = db.Users
            .Include(u => u.UserRoles)
            .Any(u => u.Email == email || u.UserRoles.Any(r => r.Role == Role.Admin));

        if (adminExists) return;

        var now = DateTime.UtcNow;
        db.Users.Add(new User
        {
            Email        = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 8),
            IsActive     = true,
            CreatedAt    = now,
            UserRoles    = [new UserRole { Role = Role.Admin, CreatedAt = now }]
        });
        db.SaveChanges();
    }

    /// <summary>
    /// Idempotently ensures a canonical set of food categories exists. Unlike <see cref="Seed"/>
    /// (which only runs on an empty database), this runs on every startup so new categories added
    /// after go-live reach databases that already have real merchant/order data.
    /// </summary>
    /// <param name="db">The database context.</param>
    public static void EnsureCategories(ResQDbContext db)
    {
        string[] canonicalNames =
        [
            "Panadería", "Sushi", "Rosticería", "Restaurante", "Vegano", "Fiambrería",
            "Pastelería", "Postres", "Pizzería", "Parrilla", "Supermercado"
        ];

        var existingNames = db.Categories.Select(c => c.Name).ToHashSet();
        var now = DateTime.UtcNow;

        var missing = canonicalNames
            .Where(name => !existingNames.Contains(name))
            .Select(name => new Category { Name = name, CreatedAt = now })
            .ToList();

        if (missing.Count == 0) return;

        db.Categories.AddRange(missing);
        db.SaveChanges();
    }

    /// <summary>
    /// Idempotently ensures a broader demo catalog exists — at least two merchants per
    /// category, each with two packs, so the consumer feed and merchant carousel have
    /// enough variety to demo well. Unlike <see cref="Seed"/> (empty-DB only), this runs
    /// on every startup and only inserts merchants that don't already exist by business name.
    /// </summary>
    /// <param name="db">The database context.</param>
    public static void EnsureDemoMerchants(ResQDbContext db)
    {
        var categories = db.Categories.ToDictionary(c => c.Name, c => c.Id);
        var existingBusinessNames = db.MerchantProfiles.Select(m => m.BusinessName).ToHashSet();
        var now = DateTime.UtcNow;
        var hash = BCrypt.Net.BCrypt.HashPassword("ResQ1234!", 8);

        // ─── Demo consumer pool ──────────────────────────────────────────────────
        // Shared by every merchant block below so each merchant's historical orders/reviews
        // have varied, realistic authors instead of a single account reviewing everything.
        var demoConsumerSeed = new (string Email, string First, string Last, string Phone)[]
        {
            ("sofia.martinez@resq.com",  "Sofía",     "Martínez",  "+54 351 555-7101"),
            ("lucas.fernandez@resq.com", "Lucas",     "Fernández", "+54 351 555-7102"),
            ("valentina.gomez@resq.com", "Valentina", "Gómez",     "+54 351 555-7103"),
            ("tomas.rodriguez@resq.com", "Tomás",     "Rodríguez", "+54 351 555-7104"),
            ("camila.lopez@resq.com",    "Camila",    "López",     "+54 351 555-7105"),
            ("franco.diaz@resq.com",     "Franco",    "Díaz",      "+54 351 555-7106"),
            ("martina.sosa@resq.com",    "Martina",   "Sosa",      "+54 351 555-7107"),
            ("nicolas.herrera@resq.com", "Nicolás",   "Herrera",   "+54 351 555-7108"),
        };

        var existingConsumerEmails = db.ConsumerProfiles.Include(c => c.User)
            .Select(c => c.User.Email).ToHashSet();

        var newConsumerUsers = demoConsumerSeed
            .Where(d => !existingConsumerEmails.Contains(d.Email))
            .Select(d => new User
            {
                Email        = d.Email,
                PasswordHash = hash,
                IsActive     = true,
                CreatedAt    = now,
                ConsumerProfile = new ConsumerProfile
                {
                    FirstName   = d.First,
                    LastName    = d.Last,
                    PhoneNumber = d.Phone,
                    CreatedAt   = now
                },
                UserRoles = [new UserRole { Role = Role.Consumer, CreatedAt = now }]
            })
            .ToList();

        if (newConsumerUsers.Count > 0)
        {
            db.Users.AddRange(newConsumerUsers);
            db.SaveChanges();
        }

        // Available to every merchant block below as demoConsumers[0..7] — reload so the
        // list is complete and stably ordered regardless of which ones were just created.
        var demoConsumers = db.ConsumerProfiles.Include(c => c.User)
            .Where(c => demoConsumerSeed.Select(d => d.Email).Contains(c.User.Email))
            .OrderBy(c => c.User.Email)
            .ToList();

        if (!existingBusinessNames.Contains("Panadería San Martín"))
        {
            var m_panaderiasanmartinUser = new User
            {
                Email        = "panaderiasanmartin@resq.com",
                PasswordHash = hash,
                IsActive     = true,
                CreatedAt    = now,
                MerchantProfile = new MerchantProfile
                {
                    BusinessName       = "Panadería San Martín",
                    Cuit               = "30-45678901-3",
                    Address            = "San Martín 850, Córdoba Centro",
                    Latitude           = -31.4179m,
                    Longitude          = -64.1841m,
                    ContactPhone       = "+54 351 555-4004",
                    MpConnectionStatus = MpConnectionStatus.Disconnected,
                    CreatedAt          = now
                },
                UserRoles = [new UserRole { Role = Role.Merchant, CreatedAt = now }]
            };
            db.Users.Add(m_panaderiasanmartinUser);
            db.SaveChanges();

            var m_panaderiasanmartin = m_panaderiasanmartinUser.MerchantProfile!;
            m_panaderiasanmartin.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/5/59/Armancette_Bakery_Indoors.jpg/960px-Armancette_Bakery_Indoors.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_panaderiasanmartin.Id, CategoryId = categories["Panadería"] });

            var m_panaderiasanmartin_p1 = new Product
            {
                MerchantId      = m_panaderiasanmartin.Id,
                Name            = "Pack Sorpresa Panadero",
                Description     = "Selección sorpresa de panes recién horneados: la mezcla exacta depende de lo que quede en el mostrador al cierre.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 1300m,
                SalePrice       = 650m,
                StockQuantity   = 6,
                PickupTimeStart = new TimeOnly(17, 30),
                PickupTimeEnd   = new TimeOnly(20, 30),
                IsActive        = true,
                CreatedAt       = now
            };
            var m_panaderiasanmartin_p2 = new Product
            {
                MerchantId      = m_panaderiasanmartin.Id,
                Name            = "Pack Facturas Surtidas",
                Description     = "Docena de facturas surtidas y recién horneadas: vigilantes, cañoncitos y sacramentos.",
                ProductType     = ProductType.ExplicitItem,
                OriginalPrice   = 900m,
                SalePrice       = 450m,
                StockQuantity   = 10,
                PickupTimeStart = new TimeOnly(18, 0),
                PickupTimeEnd   = new TimeOnly(21, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            db.Products.AddRange(m_panaderiasanmartin_p1, m_panaderiasanmartin_p2);
            db.SaveChanges();

            m_panaderiasanmartin_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/2/2c/New_bake_croissants_in_clipper_lounge.jpg/960px-New_bake_croissants_in_clipper_lounge.jpg";
            m_panaderiasanmartin_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/3/39/Facturas_pastelera.jpg/960px-Facturas_pastelera.jpg";
            db.SaveChanges();

            // ─── Reviews ──────────────────────────────────────────────────────────
            var orderPsm1 = new Order { ConsumerId = demoConsumers[0].Id, MerchantId = m_panaderiasanmartin.Id, TotalAmount = 450m, PlatformFee = 45m, MerchantEarnings = 405m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PSM-01", CreatedAt = now.AddDays(-8) };
            var orderPsm2 = new Order { ConsumerId = demoConsumers[3].Id, MerchantId = m_panaderiasanmartin.Id, TotalAmount = 650m, PlatformFee = 65m, MerchantEarnings = 585m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PSM-02", CreatedAt = now.AddDays(-15) };
            var orderPsm3 = new Order { ConsumerId = demoConsumers[5].Id, MerchantId = m_panaderiasanmartin.Id, TotalAmount = 450m, PlatformFee = 45m, MerchantEarnings = 405m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PSM-03", CreatedAt = now.AddDays(-22) };
            var orderPsm4 = new Order { ConsumerId = demoConsumers[1].Id, MerchantId = m_panaderiasanmartin.Id, TotalAmount = 650m, PlatformFee = 65m, MerchantEarnings = 585m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PSM-04", CreatedAt = now.AddDays(-5) };
            var orderPsm5 = new Order { ConsumerId = demoConsumers[6].Id, MerchantId = m_panaderiasanmartin.Id, TotalAmount = 450m, PlatformFee = 45m, MerchantEarnings = 405m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PSM-05", CreatedAt = now.AddDays(-30) };
            db.Orders.AddRange(orderPsm1, orderPsm2, orderPsm3, orderPsm4, orderPsm5);
            db.SaveChanges();

            db.OrderDetails.AddRange(
                new OrderDetail { OrderId = orderPsm1.Id, ProductId = m_panaderiasanmartin_p2.Id, Quantity = 1, UnitPrice = 450m },
                new OrderDetail { OrderId = orderPsm2.Id, ProductId = m_panaderiasanmartin_p1.Id, Quantity = 1, UnitPrice = 650m },
                new OrderDetail { OrderId = orderPsm3.Id, ProductId = m_panaderiasanmartin_p2.Id, Quantity = 1, UnitPrice = 450m },
                new OrderDetail { OrderId = orderPsm4.Id, ProductId = m_panaderiasanmartin_p1.Id, Quantity = 1, UnitPrice = 650m },
                new OrderDetail { OrderId = orderPsm5.Id, ProductId = m_panaderiasanmartin_p2.Id, Quantity = 1, UnitPrice = 450m }
            );
            db.SaveChanges();

            db.Reviews.AddRange(
                new Review { OrderId = orderPsm1.Id, MerchantId = m_panaderiasanmartin.Id, Rating = 5, Comment = "Las facturas están recién horneadas, buenísimas y tibias todavía.", CreatedAt = orderPsm1.CreatedAt },
                new Review { OrderId = orderPsm2.Id, MerchantId = m_panaderiasanmartin.Id, Rating = 4, Comment = "Buena relación precio-calidad el pack sorpresa panadero, vino con un pan de campo riquísimo.", CreatedAt = orderPsm2.CreatedAt },
                new Review { OrderId = orderPsm3.Id, MerchantId = m_panaderiasanmartin.Id, Rating = 3, Comment = "Fui un poco tarde y ya casi no quedaban facturas variadas, pero lo que había estaba bueno.", CreatedAt = orderPsm3.CreatedAt },
                new Review { OrderId = orderPsm4.Id, MerchantId = m_panaderiasanmartin.Id, Rating = 5, Comment = null, CreatedAt = orderPsm4.CreatedAt },
                new Review { OrderId = orderPsm5.Id, MerchantId = m_panaderiasanmartin.Id, Rating = 4, Comment = "Siempre pido acá, nunca decepciona.", CreatedAt = orderPsm5.CreatedAt }
            );
            db.SaveChanges();
        }

        if (!existingBusinessNames.Contains("Sushi Yamato"))
        {
            var m_sushiyamatoUser = new User
            {
                Email        = "sushiyamato@resq.com",
                PasswordHash = hash,
                IsActive     = true,
                CreatedAt    = now,
                MerchantProfile = new MerchantProfile
                {
                    BusinessName       = "Sushi Yamato",
                    Cuit               = "30-56789012-4",
                    Address            = "Av. Rafael Núñez 3200, Cerro de las Rosas",
                    Latitude           = -31.3897m,
                    Longitude          = -64.2103m,
                    ContactPhone       = "+54 351 555-5005",
                    MpConnectionStatus = MpConnectionStatus.Disconnected,
                    CreatedAt          = now
                },
                UserRoles = [new UserRole { Role = Role.Merchant, CreatedAt = now }]
            };
            db.Users.Add(m_sushiyamatoUser);
            db.SaveChanges();

            var m_sushiyamato = m_sushiyamatoUser.MerchantProfile!;
            m_sushiyamato.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/97/Sushi_Masa_by_Ki-setsu_Interior_Omakase_Counter.jpg/960px-Sushi_Masa_by_Ki-setsu_Interior_Omakase_Counter.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_sushiyamato.Id, CategoryId = categories["Sushi"] });

            var m_sushiyamato_p1 = new Product
            {
                MerchantId      = m_sushiyamato.Id,
                Name            = "Pack Yamato Mixto",
                Description     = "Selección sorpresa de 24 piezas de sushi variado, armada por la cocina entre nigiri, rolls y sashimi del día.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 3200m,
                SalePrice       = 1600m,
                StockQuantity   = 4,
                PickupTimeStart = new TimeOnly(20, 0),
                PickupTimeEnd   = new TimeOnly(22, 30),
                IsActive        = true,
                CreatedAt       = now
            };
            var m_sushiyamato_p2 = new Product
            {
                MerchantId      = m_sushiyamato.Id,
                Name            = "Pack Temaki Sorpresa",
                Description     = "Selección sorpresa de temakis surtidos, armados con el pescado más fresco del día.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 2400m,
                SalePrice       = 1200m,
                StockQuantity   = 5,
                PickupTimeStart = new TimeOnly(19, 30),
                PickupTimeEnd   = new TimeOnly(22, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            db.Products.AddRange(m_sushiyamato_p1, m_sushiyamato_p2);
            db.SaveChanges();

            m_sushiyamato_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/6/60/Sushi_platter.jpg/960px-Sushi_platter.jpg";
            m_sushiyamato_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/2/2f/Temaki-zushi.jpg/960px-Temaki-zushi.jpg";
            db.SaveChanges();

            // ─── Reviews ──────────────────────────────────────────────────────────
            var orderSy1 = new Order { ConsumerId = demoConsumers[2].Id, MerchantId = m_sushiyamato.Id, TotalAmount = 1600m, PlatformFee = 160m, MerchantEarnings = 1440m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-SY-01", CreatedAt = now.AddDays(-6) };
            var orderSy2 = new Order { ConsumerId = demoConsumers[7].Id, MerchantId = m_sushiyamato.Id, TotalAmount = 1200m, PlatformFee = 120m, MerchantEarnings = 1080m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-SY-02", CreatedAt = now.AddDays(-12) };
            var orderSy3 = new Order { ConsumerId = demoConsumers[4].Id, MerchantId = m_sushiyamato.Id, TotalAmount = 1200m, PlatformFee = 120m, MerchantEarnings = 1080m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-SY-03", CreatedAt = now.AddDays(-20) };
            var orderSy4 = new Order { ConsumerId = demoConsumers[0].Id, MerchantId = m_sushiyamato.Id, TotalAmount = 1200m, PlatformFee = 120m, MerchantEarnings = 1080m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-SY-04", CreatedAt = now.AddDays(-28) };
            var orderSy5 = new Order { ConsumerId = demoConsumers[6].Id, MerchantId = m_sushiyamato.Id, TotalAmount = 1600m, PlatformFee = 160m, MerchantEarnings = 1440m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-SY-05", CreatedAt = now.AddDays(-40) };
            db.Orders.AddRange(orderSy1, orderSy2, orderSy3, orderSy4, orderSy5);
            db.SaveChanges();

            db.OrderDetails.AddRange(
                new OrderDetail { OrderId = orderSy1.Id, ProductId = m_sushiyamato_p1.Id, Quantity = 1, UnitPrice = 1600m },
                new OrderDetail { OrderId = orderSy2.Id, ProductId = m_sushiyamato_p2.Id, Quantity = 1, UnitPrice = 1200m },
                new OrderDetail { OrderId = orderSy3.Id, ProductId = m_sushiyamato_p2.Id, Quantity = 1, UnitPrice = 1200m },
                new OrderDetail { OrderId = orderSy4.Id, ProductId = m_sushiyamato_p2.Id, Quantity = 1, UnitPrice = 1200m },
                new OrderDetail { OrderId = orderSy5.Id, ProductId = m_sushiyamato_p1.Id, Quantity = 1, UnitPrice = 1600m }
            );
            db.SaveChanges();

            db.Reviews.AddRange(
                new Review { OrderId = orderSy1.Id, MerchantId = m_sushiyamato.Id, Rating = 5, Comment = "El pack mixto vino con nigiris y sashimi increíbles, se nota que es pescado fresco.", CreatedAt = orderSy1.CreatedAt },
                new Review { OrderId = orderSy2.Id, MerchantId = m_sushiyamato.Id, Rating = 5, Comment = null, CreatedAt = orderSy2.CreatedAt },
                new Review { OrderId = orderSy3.Id, MerchantId = m_sushiyamato.Id, Rating = 4, Comment = "Los temakis estaban ricos, aunque me hubiese gustado un poco más de cantidad.", CreatedAt = orderSy3.CreatedAt },
                new Review { OrderId = orderSy4.Id, MerchantId = m_sushiyamato.Id, Rating = 3, Comment = "Llegué sobre la hora y ya no quedaba el pack mixto, tuve que llevarme el de temaki.", CreatedAt = orderSy4.CreatedAt },
                new Review { OrderId = orderSy5.Id, MerchantId = m_sushiyamato.Id, Rating = 5, Comment = "Excelente relación precio-calidad, mejor que muchos sushi delivery.", CreatedAt = orderSy5.CreatedAt }
            );
            db.SaveChanges();
        }

        if (!existingBusinessNames.Contains("Rosticería del Boulevard"))
        {
            var m_cafedelboulevardUser = new User
            {
                Email        = "rosticeriadelboulevard@resq.com",
                PasswordHash = hash,
                IsActive     = true,
                CreatedAt    = now,
                MerchantProfile = new MerchantProfile
                {
                    BusinessName       = "Rosticería del Boulevard",
                    Cuit               = "30-67890123-5",
                    Address            = "Bv. Chacabuco 450, Nueva Córdoba",
                    Latitude           = -31.4231m,
                    Longitude          = -64.1889m,
                    ContactPhone       = "+54 351 555-6006",
                    MpConnectionStatus = MpConnectionStatus.Disconnected,
                    CreatedAt          = now
                },
                UserRoles = [new UserRole { Role = Role.Merchant, CreatedAt = now }]
            };
            db.Users.Add(m_cafedelboulevardUser);
            db.SaveChanges();

            var m_cafedelboulevard = m_cafedelboulevardUser.MerchantProfile!;
            m_cafedelboulevard.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/a/ab/Rotisserie_in_Dieppe_Market_2026-05-09.jpg/960px-Rotisserie_in_Dieppe_Market_2026-05-09.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_cafedelboulevard.Id, CategoryId = categories["Rosticería"] });

            var m_cafedelboulevard_p1 = new Product
            {
                MerchantId      = m_cafedelboulevard.Id,
                Name            = "Pack Pollo al Spiedo",
                Description     = "Medio pollo al spiedo dorado y jugoso, listo para retirar con guarnición del día.",
                ProductType     = ProductType.ExplicitItem,
                OriginalPrice   = 3000m,
                SalePrice       = 1500m,
                StockQuantity   = 6,
                PickupTimeStart = new TimeOnly(18, 30),
                PickupTimeEnd   = new TimeOnly(21, 30),
                IsActive        = true,
                CreatedAt       = now
            };
            var m_cafedelboulevard_p2 = new Product
            {
                MerchantId      = m_cafedelboulevard.Id,
                Name            = "Pack Empanadas Surtidas x12",
                Description     = "Docena de empanadas surtidas: carne, jamón y queso, y verdura, recién horneadas.",
                ProductType     = ProductType.ExplicitItem,
                OriginalPrice   = 2200m,
                SalePrice       = 1100m,
                StockQuantity   = 8,
                PickupTimeStart = new TimeOnly(18, 0),
                PickupTimeEnd   = new TimeOnly(21, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            db.Products.AddRange(m_cafedelboulevard_p1, m_cafedelboulevard_p2);
            db.SaveChanges();

            m_cafedelboulevard_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/1/1b/Roasted_Chicken_Dinner_Plate%2C_Broccoli%2C_Demi_Glace.jpg/960px-Roasted_Chicken_Dinner_Plate%2C_Broccoli%2C_Demi_Glace.jpg";
            m_cafedelboulevard_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/e/ea/Golden_brown_fried_Argentine_empanadas_on_a_platter.jpg/960px-Golden_brown_fried_Argentine_empanadas_on_a_platter.jpg";
            db.SaveChanges();

            // ─── Reviews ──────────────────────────────────────────────────────────
            var orderRb1 = new Order { ConsumerId = demoConsumers[1].Id, MerchantId = m_cafedelboulevard.Id, TotalAmount = 1500m, PlatformFee = 150m, MerchantEarnings = 1350m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-RB-01", CreatedAt = now.AddDays(-7) };
            var orderRb2 = new Order { ConsumerId = demoConsumers[3].Id, MerchantId = m_cafedelboulevard.Id, TotalAmount = 1100m, PlatformFee = 110m, MerchantEarnings = 990m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-RB-02", CreatedAt = now.AddDays(-14) };
            var orderRb3 = new Order { ConsumerId = demoConsumers[5].Id, MerchantId = m_cafedelboulevard.Id, TotalAmount = 1100m, PlatformFee = 110m, MerchantEarnings = 990m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-RB-03", CreatedAt = now.AddDays(-25) };
            var orderRb4 = new Order { ConsumerId = demoConsumers[7].Id, MerchantId = m_cafedelboulevard.Id, TotalAmount = 1500m, PlatformFee = 150m, MerchantEarnings = 1350m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-RB-04", CreatedAt = now.AddDays(-18) };
            var orderRb5 = new Order { ConsumerId = demoConsumers[2].Id, MerchantId = m_cafedelboulevard.Id, TotalAmount = 1100m, PlatformFee = 110m, MerchantEarnings = 990m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-RB-05", CreatedAt = now.AddDays(-35) };
            db.Orders.AddRange(orderRb1, orderRb2, orderRb3, orderRb4, orderRb5);
            db.SaveChanges();

            db.OrderDetails.AddRange(
                new OrderDetail { OrderId = orderRb1.Id, ProductId = m_cafedelboulevard_p1.Id, Quantity = 1, UnitPrice = 1500m },
                new OrderDetail { OrderId = orderRb2.Id, ProductId = m_cafedelboulevard_p2.Id, Quantity = 1, UnitPrice = 1100m },
                new OrderDetail { OrderId = orderRb3.Id, ProductId = m_cafedelboulevard_p2.Id, Quantity = 1, UnitPrice = 1100m },
                new OrderDetail { OrderId = orderRb4.Id, ProductId = m_cafedelboulevard_p1.Id, Quantity = 1, UnitPrice = 1500m },
                new OrderDetail { OrderId = orderRb5.Id, ProductId = m_cafedelboulevard_p2.Id, Quantity = 1, UnitPrice = 1100m }
            );
            db.SaveChanges();

            db.Reviews.AddRange(
                new Review { OrderId = orderRb1.Id, MerchantId = m_cafedelboulevard.Id, Rating = 5, Comment = "El pollo al spiedo llegó dorado y jugoso, tal como prometían.", CreatedAt = orderRb1.CreatedAt },
                new Review { OrderId = orderRb2.Id, MerchantId = m_cafedelboulevard.Id, Rating = 4, Comment = null, CreatedAt = orderRb2.CreatedAt },
                new Review { OrderId = orderRb3.Id, MerchantId = m_cafedelboulevard.Id, Rating = 3, Comment = "Las empanadas estaban buenas pero el pack venía con menos cantidad de la que esperaba.", CreatedAt = orderRb3.CreatedAt },
                new Review { OrderId = orderRb4.Id, MerchantId = m_cafedelboulevard.Id, Rating = 5, Comment = "Muy rico, todo fresco.", CreatedAt = orderRb4.CreatedAt },
                new Review { OrderId = orderRb5.Id, MerchantId = m_cafedelboulevard.Id, Rating = 4, Comment = "Buena porción y buen precio, volvería a pedir.", CreatedAt = orderRb5.CreatedAt }
            );
            db.SaveChanges();
        }

        if (!existingBusinessNames.Contains("Bistró Nueva Córdoba"))
        {
            var m_bistronuevacordobaUser = new User
            {
                Email        = "bistronuevacordoba@resq.com",
                PasswordHash = hash,
                IsActive     = true,
                CreatedAt    = now,
                MerchantProfile = new MerchantProfile
                {
                    BusinessName       = "Bistró Nueva Córdoba",
                    Cuit               = "30-70123456-6",
                    Address            = "Av. Hipólito Yrigoyen 550, Nueva Córdoba",
                    Latitude           = -31.4265m,
                    Longitude          = -64.1858m,
                    ContactPhone       = "+54 351 555-7001",
                    MpConnectionStatus = MpConnectionStatus.Disconnected,
                    CreatedAt          = now
                },
                UserRoles = [new UserRole { Role = Role.Merchant, CreatedAt = now }]
            };
            db.Users.Add(m_bistronuevacordobaUser);
            db.SaveChanges();

            var m_bistronuevacordoba = m_bistronuevacordobaUser.MerchantProfile!;
            m_bistronuevacordoba.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/2/2a/Jump_Restaurant_Private_Dining_Room_%289160005659%29.jpg/960px-Jump_Restaurant_Private_Dining_Room_%289160005659%29.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_bistronuevacordoba.Id, CategoryId = categories["Restaurante"] });

            var m_bistronuevacordoba_p1 = new Product
            {
                MerchantId      = m_bistronuevacordoba.Id,
                Name            = "Pack Sorpresa del Chef",
                Description     = "Plato principal sorpresa del chef, según lo que quede del servicio de la noche.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 2500m,
                SalePrice       = 1250m,
                StockQuantity   = 5,
                PickupTimeStart = new TimeOnly(21, 0),
                PickupTimeEnd   = new TimeOnly(23, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            var m_bistronuevacordoba_p2 = new Product
            {
                MerchantId      = m_bistronuevacordoba.Id,
                Name            = "Pack Almuerzo Ejecutivo",
                Description     = "Menú ejecutivo sorpresa: entrada y plato principal armados por la cocina con lo mejor del día.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 1800m,
                SalePrice       = 900m,
                StockQuantity   = 6,
                PickupTimeStart = new TimeOnly(13, 0),
                PickupTimeEnd   = new TimeOnly(15, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            db.Products.AddRange(m_bistronuevacordoba_p1, m_bistronuevacordoba_p2);
            db.SaveChanges();

            m_bistronuevacordoba_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/d/d0/Burger_and_fries_on_a_wooden_plate.jpg/960px-Burger_and_fries_on_a_wooden_plate.jpg";
            m_bistronuevacordoba_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/94/Delicious_gourmet_dish_served_at_a_restaurant_highlighting_tender_meat_undefined.jpg/960px-Delicious_gourmet_dish_served_at_a_restaurant_highlighting_tender_meat_undefined.jpg";
            db.SaveChanges();

            // ─── Reviews ──────────────────────────────────────────────────────────
            var orderBnc1 = new Order { ConsumerId = demoConsumers[0].Id, MerchantId = m_bistronuevacordoba.Id, TotalAmount = 1250m, PlatformFee = 125m, MerchantEarnings = 1125m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-BNC-01", CreatedAt = now.AddDays(-9) };
            var orderBnc2 = new Order { ConsumerId = demoConsumers[4].Id, MerchantId = m_bistronuevacordoba.Id, TotalAmount = 900m, PlatformFee = 90m, MerchantEarnings = 810m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-BNC-02", CreatedAt = now.AddDays(-16) };
            var orderBnc3 = new Order { ConsumerId = demoConsumers[6].Id, MerchantId = m_bistronuevacordoba.Id, TotalAmount = 900m, PlatformFee = 90m, MerchantEarnings = 810m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-BNC-03", CreatedAt = now.AddDays(-27) };
            var orderBnc4 = new Order { ConsumerId = demoConsumers[2].Id, MerchantId = m_bistronuevacordoba.Id, TotalAmount = 1250m, PlatformFee = 125m, MerchantEarnings = 1125m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-BNC-04", CreatedAt = now.AddDays(-20) };
            var orderBnc5 = new Order { ConsumerId = demoConsumers[7].Id, MerchantId = m_bistronuevacordoba.Id, TotalAmount = 1250m, PlatformFee = 125m, MerchantEarnings = 1125m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-BNC-05", CreatedAt = now.AddDays(-40) };
            db.Orders.AddRange(orderBnc1, orderBnc2, orderBnc3, orderBnc4, orderBnc5);
            db.SaveChanges();

            db.OrderDetails.AddRange(
                new OrderDetail { OrderId = orderBnc1.Id, ProductId = m_bistronuevacordoba_p1.Id, Quantity = 1, UnitPrice = 1250m },
                new OrderDetail { OrderId = orderBnc2.Id, ProductId = m_bistronuevacordoba_p2.Id, Quantity = 1, UnitPrice = 900m },
                new OrderDetail { OrderId = orderBnc3.Id, ProductId = m_bistronuevacordoba_p2.Id, Quantity = 1, UnitPrice = 900m },
                new OrderDetail { OrderId = orderBnc4.Id, ProductId = m_bistronuevacordoba_p1.Id, Quantity = 1, UnitPrice = 1250m },
                new OrderDetail { OrderId = orderBnc5.Id, ProductId = m_bistronuevacordoba_p1.Id, Quantity = 1, UnitPrice = 1250m }
            );
            db.SaveChanges();

            db.Reviews.AddRange(
                new Review { OrderId = orderBnc1.Id, MerchantId = m_bistronuevacordoba.Id, Rating = 5, Comment = "El plato sorpresa del chef fue una carne exquisita con guarnición de vegetales, mejor de lo que esperaba.", CreatedAt = orderBnc1.CreatedAt },
                new Review { OrderId = orderBnc2.Id, MerchantId = m_bistronuevacordoba.Id, Rating = 4, Comment = null, CreatedAt = orderBnc2.CreatedAt },
                new Review { OrderId = orderBnc3.Id, MerchantId = m_bistronuevacordoba.Id, Rating = 2, Comment = "El almuerzo ejecutivo tardó bastante en estar listo para retirar y la porción era chica.", CreatedAt = orderBnc3.CreatedAt },
                new Review { OrderId = orderBnc4.Id, MerchantId = m_bistronuevacordoba.Id, Rating = 5, Comment = "Comida de nivel a mitad de precio, la app funciona perfecto para esto.", CreatedAt = orderBnc4.CreatedAt },
                new Review { OrderId = orderBnc5.Id, MerchantId = m_bistronuevacordoba.Id, Rating = 4, Comment = "Rico, aunque hubiese preferido saber un poco más de qué tipo de plato venía.", CreatedAt = orderBnc5.CreatedAt }
            );
            db.SaveChanges();
        }

        if (!existingBusinessNames.Contains("La Mesa Restaurante"))
        {
            var m_lamesarestauranteUser = new User
            {
                Email        = "lamesarestaurante@resq.com",
                PasswordHash = hash,
                IsActive     = true,
                CreatedAt    = now,
                MerchantProfile = new MerchantProfile
                {
                    BusinessName       = "La Mesa Restaurante",
                    Cuit               = "30-71234567-7",
                    Address            = "Av. Colón 1200, Córdoba Centro",
                    Latitude           = -31.4103m,
                    Longitude          = -64.1985m,
                    ContactPhone       = "+54 351 555-7002",
                    MpConnectionStatus = MpConnectionStatus.Disconnected,
                    CreatedAt          = now
                },
                UserRoles = [new UserRole { Role = Role.Merchant, CreatedAt = now }]
            };
            db.Users.Add(m_lamesarestauranteUser);
            db.SaveChanges();

            var m_lamesarestaurante = m_lamesarestauranteUser.MerchantProfile!;
            m_lamesarestaurante.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/f/f3/Restaurant_room_of_Amantaka_luxury_Resort_%26_Hotel_in_Luang_Prabang_Laos.jpg/960px-Restaurant_room_of_Amantaka_luxury_Resort_%26_Hotel_in_Luang_Prabang_Laos.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_lamesarestaurante.Id, CategoryId = categories["Restaurante"] });

            var m_lamesarestaurante_p1 = new Product
            {
                MerchantId      = m_lamesarestaurante.Id,
                Name            = "Pack Cena Sorpresa",
                Description     = "Selección sorpresa de platos del día, a punto de terminar el servicio de la cena.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 2200m,
                SalePrice       = 1100m,
                StockQuantity   = 4,
                PickupTimeStart = new TimeOnly(21, 30),
                PickupTimeEnd   = new TimeOnly(23, 30),
                IsActive        = true,
                CreatedAt       = now
            };
            var m_lamesarestaurante_p2 = new Product
            {
                MerchantId      = m_lamesarestaurante.Id,
                Name            = "Pack Menú del Día",
                Description     = "Menú del día sorpresa: entrada, plato principal y postre, elegidos por la cocina.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 2000m,
                SalePrice       = 1000m,
                StockQuantity   = 5,
                PickupTimeStart = new TimeOnly(12, 30),
                PickupTimeEnd   = new TimeOnly(15, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            db.Products.AddRange(m_lamesarestaurante_p1, m_lamesarestaurante_p2);
            db.SaveChanges();

            m_lamesarestaurante_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/8/87/Dish_of_meatloaf_served_on_a_white_plate_with_sauce_and_herbs_in_a_restaurant_setting.jpg/960px-Dish_of_meatloaf_served_on_a_white_plate_with_sauce_and_herbs_in_a_restaurant_setting.jpg";
            m_lamesarestaurante_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/98/Grilled_chicken_served_with_creamy_mashed_potatoes_and_fresh_vegetables_undefined.jpg/960px-Grilled_chicken_served_with_creamy_mashed_potatoes_and_fresh_vegetables_undefined.jpg";
            db.SaveChanges();

            // ─── Reviews ──────────────────────────────────────────────────────────
            var orderLmr1 = new Order { ConsumerId = demoConsumers[3].Id, MerchantId = m_lamesarestaurante.Id, TotalAmount = 1000m, PlatformFee = 100m, MerchantEarnings = 900m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-LMR-01", CreatedAt = now.AddDays(-10) };
            var orderLmr2 = new Order { ConsumerId = demoConsumers[5].Id, MerchantId = m_lamesarestaurante.Id, TotalAmount = 1100m, PlatformFee = 110m, MerchantEarnings = 990m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-LMR-02", CreatedAt = now.AddDays(-17) };
            var orderLmr3 = new Order { ConsumerId = demoConsumers[1].Id, MerchantId = m_lamesarestaurante.Id, TotalAmount = 1100m, PlatformFee = 110m, MerchantEarnings = 990m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-LMR-03", CreatedAt = now.AddDays(-32) };
            var orderLmr4 = new Order { ConsumerId = demoConsumers[6].Id, MerchantId = m_lamesarestaurante.Id, TotalAmount = 1000m, PlatformFee = 100m, MerchantEarnings = 900m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-LMR-04", CreatedAt = now.AddDays(-23) };
            var orderLmr5 = new Order { ConsumerId = demoConsumers[0].Id, MerchantId = m_lamesarestaurante.Id, TotalAmount = 1100m, PlatformFee = 110m, MerchantEarnings = 990m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-LMR-05", CreatedAt = now.AddDays(-44) };
            db.Orders.AddRange(orderLmr1, orderLmr2, orderLmr3, orderLmr4, orderLmr5);
            db.SaveChanges();

            db.OrderDetails.AddRange(
                new OrderDetail { OrderId = orderLmr1.Id, ProductId = m_lamesarestaurante_p2.Id, Quantity = 1, UnitPrice = 1000m },
                new OrderDetail { OrderId = orderLmr2.Id, ProductId = m_lamesarestaurante_p1.Id, Quantity = 1, UnitPrice = 1100m },
                new OrderDetail { OrderId = orderLmr3.Id, ProductId = m_lamesarestaurante_p1.Id, Quantity = 1, UnitPrice = 1100m },
                new OrderDetail { OrderId = orderLmr4.Id, ProductId = m_lamesarestaurante_p2.Id, Quantity = 1, UnitPrice = 1000m },
                new OrderDetail { OrderId = orderLmr5.Id, ProductId = m_lamesarestaurante_p1.Id, Quantity = 1, UnitPrice = 1100m }
            );
            db.SaveChanges();

            db.Reviews.AddRange(
                new Review { OrderId = orderLmr1.Id, MerchantId = m_lamesarestaurante.Id, Rating = 5, Comment = "El menú del día vino completo: entrada, plato principal y un postre buenísimo.", CreatedAt = orderLmr1.CreatedAt },
                new Review { OrderId = orderLmr2.Id, MerchantId = m_lamesarestaurante.Id, Rating = 5, Comment = null, CreatedAt = orderLmr2.CreatedAt },
                new Review { OrderId = orderLmr3.Id, MerchantId = m_lamesarestaurante.Id, Rating = 3, Comment = "Buena comida pero el local estaba lleno y tardamos en que nos atiendan para retirar.", CreatedAt = orderLmr3.CreatedAt },
                new Review { OrderId = orderLmr4.Id, MerchantId = m_lamesarestaurante.Id, Rating = 4, Comment = "Muy rico, todo fresco.", CreatedAt = orderLmr4.CreatedAt },
                new Review { OrderId = orderLmr5.Id, MerchantId = m_lamesarestaurante.Id, Rating = 4, Comment = "Buena opción para la cena, se nota que cuidan la presentación.", CreatedAt = orderLmr5.CreatedAt }
            );
            db.SaveChanges();
        }

        if (!existingBusinessNames.Contains("Verde Vida Vegano"))
        {
            var m_verdevidaveganoUser = new User
            {
                Email        = "verdevidavegano@resq.com",
                PasswordHash = hash,
                IsActive     = true,
                CreatedAt    = now,
                MerchantProfile = new MerchantProfile
                {
                    BusinessName       = "Verde Vida Vegano",
                    Cuit               = "30-72345678-8",
                    Address            = "Belgrano 620, Güemes",
                    Latitude           = -31.4218m,
                    Longitude          = -64.1832m,
                    ContactPhone       = "+54 351 555-7003",
                    MpConnectionStatus = MpConnectionStatus.Disconnected,
                    CreatedAt          = now
                },
                UserRoles = [new UserRole { Role = Role.Merchant, CreatedAt = now }]
            };
            db.Users.Add(m_verdevidaveganoUser);
            db.SaveChanges();

            var m_verdevidavegano = m_verdevidaveganoUser.MerchantProfile!;
            m_verdevidavegano.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/e/e5/Inside_the_La_Raposa_Transfeminist_Bookshop_and_Vegan_Restaurant%2C_Barcelona_13.jpg/960px-Inside_the_La_Raposa_Transfeminist_Bookshop_and_Vegan_Restaurant%2C_Barcelona_13.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_verdevidavegano.Id, CategoryId = categories["Vegano"] });

            var m_verdevidavegano_p1 = new Product
            {
                MerchantId      = m_verdevidavegano.Id,
                Name            = "Pack Bowl Sorpresa",
                Description     = "Selección sorpresa de bowls veganos armados con lo mejor que quedó del día: la combinación de vegetales de estación, legumbres y salsas caseras cambia cada vez que abrís el pack.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 1400m,
                SalePrice       = 700m,
                StockQuantity   = 7,
                PickupTimeStart = new TimeOnly(12, 0),
                PickupTimeEnd   = new TimeOnly(14, 30),
                IsActive        = true,
                CreatedAt       = now
            };
            var m_verdevidavegano_p2 = new Product
            {
                MerchantId      = m_verdevidavegano.Id,
                Name            = "Pack Tostadas Veganas",
                Description     = "Tostadas de masa madre con paltas y vegetales frescos.",
                ProductType     = ProductType.ExplicitItem,
                OriginalPrice   = 1100m,
                SalePrice       = 550m,
                StockQuantity   = 6,
                PickupTimeStart = new TimeOnly(9, 0),
                PickupTimeEnd   = new TimeOnly(11, 30),
                IsActive        = true,
                CreatedAt       = now
            };
            db.Products.AddRange(m_verdevidavegano_p1, m_verdevidavegano_p2);
            db.SaveChanges();

            m_verdevidavegano_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/7/71/Healthy_Lentil_Salad_%28Unsplash%29.jpg/960px-Healthy_Lentil_Salad_%28Unsplash%29.jpg";
            m_verdevidavegano_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/6/6c/Avocado_toast_with_sesame_seeds.jpg/960px-Avocado_toast_with_sesame_seeds.jpg";
            db.SaveChanges();

            var orderVvv1 = new Order { ConsumerId = demoConsumers[0].Id, MerchantId = m_verdevidavegano.Id, TotalAmount = 550m, PlatformFee = 55m, MerchantEarnings = 495m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-VVV-01", CreatedAt = now.AddDays(-6) };
            var orderVvv2 = new Order { ConsumerId = demoConsumers[3].Id, MerchantId = m_verdevidavegano.Id, TotalAmount = 700m, PlatformFee = 70m, MerchantEarnings = 630m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-VVV-02", CreatedAt = now.AddDays(-12) };
            var orderVvv3 = new Order { ConsumerId = demoConsumers[5].Id, MerchantId = m_verdevidavegano.Id, TotalAmount = 700m, PlatformFee = 70m, MerchantEarnings = 630m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-VVV-03", CreatedAt = now.AddDays(-20) };
            var orderVvv4 = new Order { ConsumerId = demoConsumers[1].Id, MerchantId = m_verdevidavegano.Id, TotalAmount = 550m, PlatformFee = 55m, MerchantEarnings = 495m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-VVV-04", CreatedAt = now.AddDays(-30) };
            var orderVvv5 = new Order { ConsumerId = demoConsumers[7].Id, MerchantId = m_verdevidavegano.Id, TotalAmount = 700m, PlatformFee = 70m, MerchantEarnings = 630m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-VVV-05", CreatedAt = now.AddDays(-40) };
            db.Orders.AddRange(orderVvv1, orderVvv2, orderVvv3, orderVvv4, orderVvv5);
            db.SaveChanges();

            db.OrderDetails.AddRange(
                new OrderDetail { OrderId = orderVvv1.Id, ProductId = m_verdevidavegano_p2.Id, Quantity = 1, UnitPrice = 550m },
                new OrderDetail { OrderId = orderVvv2.Id, ProductId = m_verdevidavegano_p1.Id, Quantity = 1, UnitPrice = 700m },
                new OrderDetail { OrderId = orderVvv3.Id, ProductId = m_verdevidavegano_p1.Id, Quantity = 1, UnitPrice = 700m },
                new OrderDetail { OrderId = orderVvv4.Id, ProductId = m_verdevidavegano_p2.Id, Quantity = 1, UnitPrice = 550m },
                new OrderDetail { OrderId = orderVvv5.Id, ProductId = m_verdevidavegano_p1.Id, Quantity = 1, UnitPrice = 700m }
            );
            db.SaveChanges();

            db.Reviews.AddRange(
                new Review { OrderId = orderVvv1.Id, MerchantId = m_verdevidavegano.Id, Rating = 5, Comment = "Las tostadas veganas estaban espectaculares, todo fresquísimo.", CreatedAt = orderVvv1.CreatedAt },
                new Review { OrderId = orderVvv2.Id, MerchantId = m_verdevidavegano.Id, Rating = 4, Comment = "Buena relación precio-calidad, el bowl vino con muchas legumbres.", CreatedAt = orderVvv2.CreatedAt },
                new Review { OrderId = orderVvv3.Id, MerchantId = m_verdevidavegano.Id, Rating = 3, Comment = "Llegué cerca del cierre y ya no quedaban muchas opciones, igual estaba rico.", CreatedAt = orderVvv3.CreatedAt },
                new Review { OrderId = orderVvv4.Id, MerchantId = m_verdevidavegano.Id, Rating = 5, Comment = null, CreatedAt = orderVvv4.CreatedAt },
                new Review { OrderId = orderVvv5.Id, MerchantId = m_verdevidavegano.Id, Rating = 5, Comment = "Muy rico, todo fresco.", CreatedAt = orderVvv5.CreatedAt }
            );
            db.SaveChanges();
        }

        if (!existingBusinessNames.Contains("Raíz Cocina Vegana"))
        {
            var m_raizcocinaveganaUser = new User
            {
                Email        = "raizcocinavegana@resq.com",
                PasswordHash = hash,
                IsActive     = true,
                CreatedAt    = now,
                MerchantProfile = new MerchantProfile
                {
                    BusinessName       = "Raíz Cocina Vegana",
                    Cuit               = "30-73456789-9",
                    Address            = "Av. Vélez Sarsfield 780, Güemes",
                    Latitude           = -31.4225m,
                    Longitude          = -64.1798m,
                    ContactPhone       = "+54 351 555-7004",
                    MpConnectionStatus = MpConnectionStatus.Disconnected,
                    CreatedAt          = now
                },
                UserRoles = [new UserRole { Role = Role.Merchant, CreatedAt = now }]
            };
            db.Users.Add(m_raizcocinaveganaUser);
            db.SaveChanges();

            var m_raizcocinavegana = m_raizcocinaveganaUser.MerchantProfile!;
            m_raizcocinavegana.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/1/15/Inside_the_La_Raposa_Transfeminist_Bookshop_and_Vegan_Restaurant%2C_Barcelona_03.jpg/960px-Inside_the_La_Raposa_Transfeminist_Bookshop_and_Vegan_Restaurant%2C_Barcelona_03.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_raizcocinavegana.Id, CategoryId = categories["Vegano"] });

            var m_raizcocinavegana_p1 = new Product
            {
                MerchantId      = m_raizcocinavegana.Id,
                Name            = "Pack Sorpresa Raíz",
                Description     = "Selección de platos veganos de estación preparados en el día.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 1600m,
                SalePrice       = 800m,
                StockQuantity   = 5,
                PickupTimeStart = new TimeOnly(20, 0),
                PickupTimeEnd   = new TimeOnly(22, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            var m_raizcocinavegana_p2 = new Product
            {
                MerchantId      = m_raizcocinavegana.Id,
                Name            = "Pack Smoothie & Bowl",
                Description     = "Smoothie de frutas + bowl de granola y frutas frescas.",
                ProductType     = ProductType.ExplicitItem,
                OriginalPrice   = 1200m,
                SalePrice       = 600m,
                StockQuantity   = 8,
                PickupTimeStart = new TimeOnly(9, 30),
                PickupTimeEnd   = new TimeOnly(12, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            db.Products.AddRange(m_raizcocinavegana_p1, m_raizcocinavegana_p2);
            db.SaveChanges();

            m_raizcocinavegana_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/f/f9/Vegan_Buddha_Bowl.jpg/960px-Vegan_Buddha_Bowl.jpg";
            m_raizcocinavegana_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/0/0e/Galaxy_Smoothie_Bowl_%28Unsplash%29.jpg/960px-Galaxy_Smoothie_Bowl_%28Unsplash%29.jpg";
            db.SaveChanges();

            var orderRcv1 = new Order { ConsumerId = demoConsumers[2].Id, MerchantId = m_raizcocinavegana.Id, TotalAmount = 800m, PlatformFee = 80m, MerchantEarnings = 720m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-RCV-01", CreatedAt = now.AddDays(-8) };
            var orderRcv2 = new Order { ConsumerId = demoConsumers[4].Id, MerchantId = m_raizcocinavegana.Id, TotalAmount = 600m, PlatformFee = 60m, MerchantEarnings = 540m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-RCV-02", CreatedAt = now.AddDays(-15) };
            var orderRcv3 = new Order { ConsumerId = demoConsumers[6].Id, MerchantId = m_raizcocinavegana.Id, TotalAmount = 800m, PlatformFee = 80m, MerchantEarnings = 720m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-RCV-03", CreatedAt = now.AddDays(-25) };
            var orderRcv4 = new Order { ConsumerId = demoConsumers[0].Id, MerchantId = m_raizcocinavegana.Id, TotalAmount = 800m, PlatformFee = 80m, MerchantEarnings = 720m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-RCV-04", CreatedAt = now.AddDays(-35) };
            var orderRcv5 = new Order { ConsumerId = demoConsumers[3].Id, MerchantId = m_raizcocinavegana.Id, TotalAmount = 600m, PlatformFee = 60m, MerchantEarnings = 540m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-RCV-05", CreatedAt = now.AddDays(-5) };
            db.Orders.AddRange(orderRcv1, orderRcv2, orderRcv3, orderRcv4, orderRcv5);
            db.SaveChanges();

            db.OrderDetails.AddRange(
                new OrderDetail { OrderId = orderRcv1.Id, ProductId = m_raizcocinavegana_p1.Id, Quantity = 1, UnitPrice = 800m },
                new OrderDetail { OrderId = orderRcv2.Id, ProductId = m_raizcocinavegana_p2.Id, Quantity = 1, UnitPrice = 600m },
                new OrderDetail { OrderId = orderRcv3.Id, ProductId = m_raizcocinavegana_p1.Id, Quantity = 1, UnitPrice = 800m },
                new OrderDetail { OrderId = orderRcv4.Id, ProductId = m_raizcocinavegana_p1.Id, Quantity = 1, UnitPrice = 800m },
                new OrderDetail { OrderId = orderRcv5.Id, ProductId = m_raizcocinavegana_p2.Id, Quantity = 1, UnitPrice = 600m }
            );
            db.SaveChanges();

            db.Reviews.AddRange(
                new Review { OrderId = orderRcv1.Id, MerchantId = m_raizcocinavegana.Id, Rating = 5, Comment = "El pack sorpresa Raíz siempre trae platos distintos, nunca me aburro.", CreatedAt = orderRcv1.CreatedAt },
                new Review { OrderId = orderRcv2.Id, MerchantId = m_raizcocinavegana.Id, Rating = 4, Comment = "El smoothie & bowl estuvo bueno, un poco chico para el precio.", CreatedAt = orderRcv2.CreatedAt },
                new Review { OrderId = orderRcv3.Id, MerchantId = m_raizcocinavegana.Id, Rating = 3, Comment = "Tuve que esperar bastante para que me entregaran el pedido, pero la comida bien.", CreatedAt = orderRcv3.CreatedAt },
                new Review { OrderId = orderRcv4.Id, MerchantId = m_raizcocinavegana.Id, Rating = 5, Comment = null, CreatedAt = orderRcv4.CreatedAt },
                new Review { OrderId = orderRcv5.Id, MerchantId = m_raizcocinavegana.Id, Rating = 4, Comment = "Rico y saludable, buena opción para el almuerzo.", CreatedAt = orderRcv5.CreatedAt }
            );
            db.SaveChanges();
        }

        if (!existingBusinessNames.Contains("Fiambrería Cremona"))
        {
            var m_heladeriacremolattiUser = new User
            {
                Email        = "fiambreriacremona@resq.com",
                PasswordHash = hash,
                IsActive     = true,
                CreatedAt    = now,
                MerchantProfile = new MerchantProfile
                {
                    BusinessName       = "Fiambrería Cremona",
                    Cuit               = "30-74567890-0",
                    Address            = "Av. Rafael Núñez 4100, Cerro de las Rosas",
                    Latitude           = -31.3856m,
                    Longitude          = -64.2145m,
                    ContactPhone       = "+54 351 555-7005",
                    MpConnectionStatus = MpConnectionStatus.Disconnected,
                    CreatedAt          = now
                },
                UserRoles = [new UserRole { Role = Role.Merchant, CreatedAt = now }]
            };
            db.Users.Add(m_heladeriacremolattiUser);
            db.SaveChanges();

            var m_heladeriacremolatti = m_heladeriacremolattiUser.MerchantProfile!;
            m_heladeriacremolatti.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/0/04/Deli_counter%2C_Delicatessen_Polonus%2C_East_Hill%2C_St_Austell%2C_Cornwall_-_November_2022.jpg/960px-Deli_counter%2C_Delicatessen_Polonus%2C_East_Hill%2C_St_Austell%2C_Cornwall_-_November_2022.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_heladeriacremolatti.Id, CategoryId = categories["Fiambrería"] });

            var m_heladeriacremolatti_p1 = new Product
            {
                MerchantId      = m_heladeriacremolatti.Id,
                Name            = "Pack Tabla de Fiambres y Quesos",
                Description     = "Tabla con jamón cocido, salame, mortadela y una selección de quesos duros y semiduros, lista para servir.",
                ProductType     = ProductType.ExplicitItem,
                OriginalPrice   = 3500m,
                SalePrice       = 1750m,
                StockQuantity   = 6,
                PickupTimeStart = new TimeOnly(17, 0),
                PickupTimeEnd   = new TimeOnly(20, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            var m_heladeriacremolatti_p2 = new Product
            {
                MerchantId      = m_heladeriacremolatti.Id,
                Name            = "Pack Jamón Crudo x250g",
                Description     = "250 g de jamón crudo estacionado, cortado fino al momento.",
                ProductType     = ProductType.ExplicitItem,
                OriginalPrice   = 1200m,
                SalePrice       = 600m,
                StockQuantity   = 10,
                PickupTimeStart = new TimeOnly(16, 30),
                PickupTimeEnd   = new TimeOnly(19, 30),
                IsActive        = true,
                CreatedAt       = now
            };
            db.Products.AddRange(m_heladeriacremolatti_p1, m_heladeriacremolatti_p2);
            db.SaveChanges();

            m_heladeriacremolatti_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/d/db/Charcuterie_board_with_various_cheeses_meats_olives_and_vegetables_arranged.jpg/960px-Charcuterie_board_with_various_cheeses_meats_olives_and_vegetables_arranged.jpg";
            m_heladeriacremolatti_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/2/22/A_plate_of_Jam%C3%B3n_serrano_in_Madrid%2C_Spain_Jam%C3%B3n_serrano_is_a_type_of_jam%C3%B3n_%28dry-cured_Spanish_ham%29%2C_which_is_generally_served_in_thin_slices.jpg/960px-A_plate_of_Jam%C3%B3n_serrano_in_Madrid%2C_Spain_Jam%C3%B3n_serrano_is_a_type_of_jam%C3%B3n_%28dry-cured_Spanish_ham%29%2C_which_is_generally_served_in_thin_slices.jpg";
            db.SaveChanges();

            var orderFc1 = new Order { ConsumerId = demoConsumers[1].Id, MerchantId = m_heladeriacremolatti.Id, TotalAmount = 1750m, PlatformFee = 175m, MerchantEarnings = 1575m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-FC-01", CreatedAt = now.AddDays(-7) };
            var orderFc2 = new Order { ConsumerId = demoConsumers[5].Id, MerchantId = m_heladeriacremolatti.Id, TotalAmount = 600m, PlatformFee = 60m, MerchantEarnings = 540m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-FC-02", CreatedAt = now.AddDays(-14) };
            var orderFc3 = new Order { ConsumerId = demoConsumers[7].Id, MerchantId = m_heladeriacremolatti.Id, TotalAmount = 1750m, PlatformFee = 175m, MerchantEarnings = 1575m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-FC-03", CreatedAt = now.AddDays(-22) };
            var orderFc4 = new Order { ConsumerId = demoConsumers[2].Id, MerchantId = m_heladeriacremolatti.Id, TotalAmount = 600m, PlatformFee = 60m, MerchantEarnings = 540m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-FC-04", CreatedAt = now.AddDays(-31) };
            var orderFc5 = new Order { ConsumerId = demoConsumers[4].Id, MerchantId = m_heladeriacremolatti.Id, TotalAmount = 1750m, PlatformFee = 175m, MerchantEarnings = 1575m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-FC-05", CreatedAt = now.AddDays(-42) };
            db.Orders.AddRange(orderFc1, orderFc2, orderFc3, orderFc4, orderFc5);
            db.SaveChanges();

            db.OrderDetails.AddRange(
                new OrderDetail { OrderId = orderFc1.Id, ProductId = m_heladeriacremolatti_p1.Id, Quantity = 1, UnitPrice = 1750m },
                new OrderDetail { OrderId = orderFc2.Id, ProductId = m_heladeriacremolatti_p2.Id, Quantity = 1, UnitPrice = 600m },
                new OrderDetail { OrderId = orderFc3.Id, ProductId = m_heladeriacremolatti_p1.Id, Quantity = 1, UnitPrice = 1750m },
                new OrderDetail { OrderId = orderFc4.Id, ProductId = m_heladeriacremolatti_p2.Id, Quantity = 1, UnitPrice = 600m },
                new OrderDetail { OrderId = orderFc5.Id, ProductId = m_heladeriacremolatti_p1.Id, Quantity = 1, UnitPrice = 1750m }
            );
            db.SaveChanges();

            db.Reviews.AddRange(
                new Review { OrderId = orderFc1.Id, MerchantId = m_heladeriacremolatti.Id, Rating = 5, Comment = "La tabla de fiambres y quesos vino generosa, ideal para compartir en casa.", CreatedAt = orderFc1.CreatedAt },
                new Review { OrderId = orderFc2.Id, MerchantId = m_heladeriacremolatti.Id, Rating = 5, Comment = null, CreatedAt = orderFc2.CreatedAt },
                new Review { OrderId = orderFc3.Id, MerchantId = m_heladeriacremolatti.Id, Rating = 3, Comment = "El pack venía más chico de lo que esperaba por el precio, aunque la calidad estaba bien.", CreatedAt = orderFc3.CreatedAt },
                new Review { OrderId = orderFc4.Id, MerchantId = m_heladeriacremolatti.Id, Rating = 4, Comment = "Buen jamón crudo, cortado fino como prometían.", CreatedAt = orderFc4.CreatedAt },
                new Review { OrderId = orderFc5.Id, MerchantId = m_heladeriacremolatti.Id, Rating = 5, Comment = "Excelente relación calidad-precio, volvería a comprar.", CreatedAt = orderFc5.CreatedAt }
            );
            db.SaveChanges();
        }

        if (!existingBusinessNames.Contains("Fiambrería del Sol"))
        {
            var m_gelatodelsolUser = new User
            {
                Email        = "fiambreriadelsol@resq.com",
                PasswordHash = hash,
                IsActive     = true,
                CreatedAt    = now,
                MerchantProfile = new MerchantProfile
                {
                    BusinessName       = "Fiambrería del Sol",
                    Cuit               = "30-75678901-1",
                    Address            = "Duarte Quirós 2100, Alberdi",
                    Latitude           = -31.4148m,
                    Longitude          = -64.2012m,
                    ContactPhone       = "+54 351 555-7006",
                    MpConnectionStatus = MpConnectionStatus.Disconnected,
                    CreatedAt          = now
                },
                UserRoles = [new UserRole { Role = Role.Merchant, CreatedAt = now }]
            };
            db.Users.Add(m_gelatodelsolUser);
            db.SaveChanges();

            var m_gelatodelsol = m_gelatodelsolUser.MerchantProfile!;
            m_gelatodelsol.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/b/b6/Versailles_Old_Shop_2011.jpg/960px-Versailles_Old_Shop_2011.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_gelatodelsol.Id, CategoryId = categories["Fiambrería"] });

            var m_gelatodelsol_p1 = new Product
            {
                MerchantId      = m_gelatodelsol.Id,
                Name            = "Pack Salame y Quesos Surtidos",
                Description     = "Salame, provolone y queso cremoso surtidos, listos para picar.",
                ProductType     = ProductType.ExplicitItem,
                OriginalPrice   = 1800m,
                SalePrice       = 900m,
                StockQuantity   = 8,
                PickupTimeStart = new TimeOnly(17, 30),
                PickupTimeEnd   = new TimeOnly(20, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            var m_gelatodelsol_p2 = new Product
            {
                MerchantId      = m_gelatodelsol.Id,
                Name            = "Pack Picada Surtida",
                Description     = "Picada con salame, jamón cocido, queso cremoso y aceitunas verdes, lista para compartir en el momento.",
                ProductType     = ProductType.ExplicitItem,
                OriginalPrice   = 900m,
                SalePrice       = 450m,
                StockQuantity   = 12,
                PickupTimeStart = new TimeOnly(17, 0),
                PickupTimeEnd   = new TimeOnly(19, 30),
                IsActive        = true,
                CreatedAt       = now
            };
            db.Products.AddRange(m_gelatodelsol_p1, m_gelatodelsol_p2);
            db.SaveChanges();

            m_gelatodelsol_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/b/ba/Charcuterie_box.jpg/960px-Charcuterie_box.jpg";
            m_gelatodelsol_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/a/a1/Tagliere_toscano.jpg/960px-Tagliere_toscano.jpg";
            db.SaveChanges();

            var orderFds1 = new Order { ConsumerId = demoConsumers[0].Id, MerchantId = m_gelatodelsol.Id, TotalAmount = 450m, PlatformFee = 45m, MerchantEarnings = 405m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-FDS-01", CreatedAt = now.AddDays(-9) };
            var orderFds2 = new Order { ConsumerId = demoConsumers[3].Id, MerchantId = m_gelatodelsol.Id, TotalAmount = 900m, PlatformFee = 90m, MerchantEarnings = 810m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-FDS-02", CreatedAt = now.AddDays(-16) };
            var orderFds3 = new Order { ConsumerId = demoConsumers[6].Id, MerchantId = m_gelatodelsol.Id, TotalAmount = 450m, PlatformFee = 45m, MerchantEarnings = 405m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-FDS-03", CreatedAt = now.AddDays(-23) };
            var orderFds4 = new Order { ConsumerId = demoConsumers[1].Id, MerchantId = m_gelatodelsol.Id, TotalAmount = 900m, PlatformFee = 90m, MerchantEarnings = 810m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-FDS-04", CreatedAt = now.AddDays(-33) };
            var orderFds5 = new Order { ConsumerId = demoConsumers[7].Id, MerchantId = m_gelatodelsol.Id, TotalAmount = 450m, PlatformFee = 45m, MerchantEarnings = 405m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-FDS-05", CreatedAt = now.AddDays(-4) };
            db.Orders.AddRange(orderFds1, orderFds2, orderFds3, orderFds4, orderFds5);
            db.SaveChanges();

            db.OrderDetails.AddRange(
                new OrderDetail { OrderId = orderFds1.Id, ProductId = m_gelatodelsol_p2.Id, Quantity = 1, UnitPrice = 450m },
                new OrderDetail { OrderId = orderFds2.Id, ProductId = m_gelatodelsol_p1.Id, Quantity = 1, UnitPrice = 900m },
                new OrderDetail { OrderId = orderFds3.Id, ProductId = m_gelatodelsol_p2.Id, Quantity = 1, UnitPrice = 450m },
                new OrderDetail { OrderId = orderFds4.Id, ProductId = m_gelatodelsol_p1.Id, Quantity = 1, UnitPrice = 900m },
                new OrderDetail { OrderId = orderFds5.Id, ProductId = m_gelatodelsol_p2.Id, Quantity = 1, UnitPrice = 450m }
            );
            db.SaveChanges();

            db.Reviews.AddRange(
                new Review { OrderId = orderFds1.Id, MerchantId = m_gelatodelsol.Id, Rating = 4, Comment = "Picada rica para picar entre dos, buena variedad de fiambres.", CreatedAt = orderFds1.CreatedAt },
                new Review { OrderId = orderFds2.Id, MerchantId = m_gelatodelsol.Id, Rating = 5, Comment = "El salame y los quesos vinieron frescos, todo muy bien cortado.", CreatedAt = orderFds2.CreatedAt },
                new Review { OrderId = orderFds3.Id, MerchantId = m_gelatodelsol.Id, Rating = 2, Comment = "Pedí temprano y cuando fui a retirar ya no quedaban aceitunas, un poco desprolijo.", CreatedAt = orderFds3.CreatedAt },
                new Review { OrderId = orderFds4.Id, MerchantId = m_gelatodelsol.Id, Rating = 5, Comment = null, CreatedAt = orderFds4.CreatedAt },
                new Review { OrderId = orderFds5.Id, MerchantId = m_gelatodelsol.Id, Rating = 4, Comment = "Todo rico, buena porción para el precio.", CreatedAt = orderFds5.CreatedAt }
            );
            db.SaveChanges();
        }

        if (!existingBusinessNames.Contains("Pastelería Dulce Trigo"))
        {
            var m_pasteleriadulcetrigoUser = new User
            {
                Email        = "pasteleriadulcetrigo@resq.com",
                PasswordHash = hash,
                IsActive     = true,
                CreatedAt    = now,
                MerchantProfile = new MerchantProfile
                {
                    BusinessName       = "Pastelería Dulce Trigo",
                    Cuit               = "30-76789012-2",
                    Address            = "Obispo Trejo 600, Córdoba Centro",
                    Latitude           = -31.4166m,
                    Longitude          = -64.1855m,
                    ContactPhone       = "+54 351 555-7007",
                    MpConnectionStatus = MpConnectionStatus.Disconnected,
                    CreatedAt          = now
                },
                UserRoles = [new UserRole { Role = Role.Merchant, CreatedAt = now }]
            };
            db.Users.Add(m_pasteleriadulcetrigoUser);
            db.SaveChanges();

            var m_pasteleriadulcetrigo = m_pasteleriadulcetrigoUser.MerchantProfile!;
            m_pasteleriadulcetrigo.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/4/4a/Cake_display_cases_with_cakes_and_pastries_in_Brastads_Bageri_2.jpg/960px-Cake_display_cases_with_cakes_and_pastries_in_Brastads_Bageri_2.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_pasteleriadulcetrigo.Id, CategoryId = categories["Pastelería"] });

            var m_pasteleriadulcetrigo_p1 = new Product
            {
                MerchantId      = m_pasteleriadulcetrigo.Id,
                Name            = "Pack Sorpresa Repostería",
                Description     = "Selección de porciones de tortas y pastelitos del día.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 1700m,
                SalePrice       = 850m,
                StockQuantity   = 6,
                PickupTimeStart = new TimeOnly(18, 0),
                PickupTimeEnd   = new TimeOnly(20, 30),
                IsActive        = true,
                CreatedAt       = now
            };
            var m_pasteleriadulcetrigo_p2 = new Product
            {
                MerchantId      = m_pasteleriadulcetrigo.Id,
                Name            = "Pack Docena Mixta",
                Description     = "12 pastelitos surtidos: hojaldre, crema y dulce de leche.",
                ProductType     = ProductType.ExplicitItem,
                OriginalPrice   = 1400m,
                SalePrice       = 700m,
                StockQuantity   = 8,
                PickupTimeStart = new TimeOnly(17, 0),
                PickupTimeEnd   = new TimeOnly(20, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            db.Products.AddRange(m_pasteleriadulcetrigo_p1, m_pasteleriadulcetrigo_p2);
            db.SaveChanges();

            m_pasteleriadulcetrigo_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/7/78/French_Pastries_%28Unsplash%29.jpg/960px-French_Pastries_%28Unsplash%29.jpg";
            m_pasteleriadulcetrigo_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/8/8d/Palmeras_de_hojaldre_1.jpg/960px-Palmeras_de_hojaldre_1.jpg";
            db.SaveChanges();

            var orderPdt1 = new Order { ConsumerId = demoConsumers[2].Id, MerchantId = m_pasteleriadulcetrigo.Id, TotalAmount = 850m, PlatformFee = 85m, MerchantEarnings = 765m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PDT-01", CreatedAt = now.AddDays(-6) };
            var orderPdt2 = new Order { ConsumerId = demoConsumers[5].Id, MerchantId = m_pasteleriadulcetrigo.Id, TotalAmount = 700m, PlatformFee = 70m, MerchantEarnings = 630m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PDT-02", CreatedAt = now.AddDays(-13) };
            var orderPdt3 = new Order { ConsumerId = demoConsumers[4].Id, MerchantId = m_pasteleriadulcetrigo.Id, TotalAmount = 850m, PlatformFee = 85m, MerchantEarnings = 765m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PDT-03", CreatedAt = now.AddDays(-21) };
            var orderPdt4 = new Order { ConsumerId = demoConsumers[0].Id, MerchantId = m_pasteleriadulcetrigo.Id, TotalAmount = 700m, PlatformFee = 70m, MerchantEarnings = 630m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PDT-04", CreatedAt = now.AddDays(-29) };
            var orderPdt5 = new Order { ConsumerId = demoConsumers[6].Id, MerchantId = m_pasteleriadulcetrigo.Id, TotalAmount = 850m, PlatformFee = 85m, MerchantEarnings = 765m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PDT-05", CreatedAt = now.AddDays(-38) };
            db.Orders.AddRange(orderPdt1, orderPdt2, orderPdt3, orderPdt4, orderPdt5);
            db.SaveChanges();

            db.OrderDetails.AddRange(
                new OrderDetail { OrderId = orderPdt1.Id, ProductId = m_pasteleriadulcetrigo_p1.Id, Quantity = 1, UnitPrice = 850m },
                new OrderDetail { OrderId = orderPdt2.Id, ProductId = m_pasteleriadulcetrigo_p2.Id, Quantity = 1, UnitPrice = 700m },
                new OrderDetail { OrderId = orderPdt3.Id, ProductId = m_pasteleriadulcetrigo_p1.Id, Quantity = 1, UnitPrice = 850m },
                new OrderDetail { OrderId = orderPdt4.Id, ProductId = m_pasteleriadulcetrigo_p2.Id, Quantity = 1, UnitPrice = 700m },
                new OrderDetail { OrderId = orderPdt5.Id, ProductId = m_pasteleriadulcetrigo_p1.Id, Quantity = 1, UnitPrice = 850m }
            );
            db.SaveChanges();

            db.Reviews.AddRange(
                new Review { OrderId = orderPdt1.Id, MerchantId = m_pasteleriadulcetrigo.Id, Rating = 5, Comment = "El pack sorpresa repostería trajo de todo: tortas, pastelitos, riquísimo.", CreatedAt = orderPdt1.CreatedAt },
                new Review { OrderId = orderPdt2.Id, MerchantId = m_pasteleriadulcetrigo.Id, Rating = 4, Comment = "La docena mixta estuvo buena, algunos pastelitos un poco secos.", CreatedAt = orderPdt2.CreatedAt },
                new Review { OrderId = orderPdt3.Id, MerchantId = m_pasteleriadulcetrigo.Id, Rating = 3, Comment = "Tuve que esperar un rato porque todavía estaban armando los packs, pero valió la pena.", CreatedAt = orderPdt3.CreatedAt },
                new Review { OrderId = orderPdt4.Id, MerchantId = m_pasteleriadulcetrigo.Id, Rating = 5, Comment = null, CreatedAt = orderPdt4.CreatedAt },
                new Review { OrderId = orderPdt5.Id, MerchantId = m_pasteleriadulcetrigo.Id, Rating = 5, Comment = "Muy rico, todo fresco.", CreatedAt = orderPdt5.CreatedAt }
            );
            db.SaveChanges();
        }

        if (!existingBusinessNames.Contains("La Repostería de Marta"))
        {
            var m_lareposteriademartaUser = new User
            {
                Email        = "lareposteriademarta@resq.com",
                PasswordHash = hash,
                IsActive     = true,
                CreatedAt    = now,
                MerchantProfile = new MerchantProfile
                {
                    BusinessName       = "La Repostería de Marta",
                    Cuit               = "30-77890123-3",
                    Address            = "Rondeau 350, Alta Córdoba",
                    Latitude           = -31.3925m,
                    Longitude          = -64.178m,
                    ContactPhone       = "+54 351 555-7008",
                    MpConnectionStatus = MpConnectionStatus.Disconnected,
                    CreatedAt          = now
                },
                UserRoles = [new UserRole { Role = Role.Merchant, CreatedAt = now }]
            };
            db.Users.Add(m_lareposteriademartaUser);
            db.SaveChanges();

            var m_lareposteriademarta = m_lareposteriademartaUser.MerchantProfile!;
            m_lareposteriademarta.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/8/86/Cake_display_cases_with_cakes_and_pastries_in_Brastads_Bageri_3.jpg/960px-Cake_display_cases_with_cakes_and_pastries_in_Brastads_Bageri_3.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_lareposteriademarta.Id, CategoryId = categories["Pastelería"] });

            var m_lareposteriademarta_p1 = new Product
            {
                MerchantId      = m_lareposteriademarta.Id,
                Name            = "Pack Sorpresa de Marta",
                Description     = "Selección sorpresa de porciones de torta y masas dulces caseras del día, recién horneadas y listas para llevar.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 1600m,
                SalePrice       = 800m,
                StockQuantity   = 5,
                PickupTimeStart = new TimeOnly(18, 30),
                PickupTimeEnd   = new TimeOnly(21, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            var m_lareposteriademarta_p2 = new Product
            {
                MerchantId      = m_lareposteriademarta.Id,
                Name            = "Pack Alfajores Surtidos",
                Description     = "Media docena de alfajores artesanales rellenos de dulce de leche: mitad bañados en chocolate, mitad con glaseado blanco.",
                ProductType     = ProductType.ExplicitItem,
                OriginalPrice   = 1000m,
                SalePrice       = 500m,
                StockQuantity   = 10,
                PickupTimeStart = new TimeOnly(16, 0),
                PickupTimeEnd   = new TimeOnly(20, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            db.Products.AddRange(m_lareposteriademarta_p1, m_lareposteriademarta_p2);
            db.SaveChanges();

            m_lareposteriademarta_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/a/a4/Pink_Cupcakes.jpg/960px-Pink_Cupcakes.jpg";
            m_lareposteriademarta_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/7/76/Alfajores_Brutales_-_2024_-_06.jpg/960px-Alfajores_Brutales_-_2024_-_06.jpg";
            db.SaveChanges();

            // ─── Reviews ──────────────────────────────────────────────────────────
            var orderLrm1 = new Order { ConsumerId = demoConsumers[1].Id, MerchantId = m_lareposteriademarta.Id, TotalAmount = 500m, PlatformFee = 50m, MerchantEarnings = 450m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-LRM-01", CreatedAt = now.AddDays(-6) };
            var orderLrm2 = new Order { ConsumerId = demoConsumers[4].Id, MerchantId = m_lareposteriademarta.Id, TotalAmount = 800m, PlatformFee = 80m, MerchantEarnings = 720m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-LRM-02", CreatedAt = now.AddDays(-13) };
            var orderLrm3 = new Order { ConsumerId = demoConsumers[7].Id, MerchantId = m_lareposteriademarta.Id, TotalAmount = 500m, PlatformFee = 50m, MerchantEarnings = 450m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-LRM-03", CreatedAt = now.AddDays(-20) };
            var orderLrm4 = new Order { ConsumerId = demoConsumers[2].Id, MerchantId = m_lareposteriademarta.Id, TotalAmount = 800m, PlatformFee = 80m, MerchantEarnings = 720m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-LRM-04", CreatedAt = now.AddDays(-28) };
            var orderLrm5 = new Order { ConsumerId = demoConsumers[5].Id, MerchantId = m_lareposteriademarta.Id, TotalAmount = 500m, PlatformFee = 50m, MerchantEarnings = 450m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-LRM-05", CreatedAt = now.AddDays(-40) };
            db.Orders.AddRange(orderLrm1, orderLrm2, orderLrm3, orderLrm4, orderLrm5);
            db.SaveChanges();

            db.OrderDetails.AddRange(
                new OrderDetail { OrderId = orderLrm1.Id, ProductId = m_lareposteriademarta_p2.Id, Quantity = 1, UnitPrice = 500m },
                new OrderDetail { OrderId = orderLrm2.Id, ProductId = m_lareposteriademarta_p1.Id, Quantity = 1, UnitPrice = 800m },
                new OrderDetail { OrderId = orderLrm3.Id, ProductId = m_lareposteriademarta_p2.Id, Quantity = 1, UnitPrice = 500m },
                new OrderDetail { OrderId = orderLrm4.Id, ProductId = m_lareposteriademarta_p1.Id, Quantity = 1, UnitPrice = 800m },
                new OrderDetail { OrderId = orderLrm5.Id, ProductId = m_lareposteriademarta_p2.Id, Quantity = 1, UnitPrice = 500m }
            );
            db.SaveChanges();

            db.Reviews.AddRange(
                new Review { OrderId = orderLrm1.Id, MerchantId = m_lareposteriademarta.Id, Rating = 5, Comment = "Los alfajores bañados en chocolate son un golazo, quedan recién hechos.", CreatedAt = orderLrm1.CreatedAt },
                new Review { OrderId = orderLrm2.Id, MerchantId = m_lareposteriademarta.Id, Rating = 4, Comment = "Buena variedad de tortas por la mitad de precio, eso sí, llegué casi al cierre y quedaba poco.", CreatedAt = orderLrm2.CreatedAt },
                new Review { OrderId = orderLrm3.Id, MerchantId = m_lareposteriademarta.Id, Rating = 3, Comment = "Rico, pero el pack trajo menos alfajores de los que esperaba para el precio.", CreatedAt = orderLrm3.CreatedAt },
                new Review { OrderId = orderLrm4.Id, MerchantId = m_lareposteriademarta.Id, Rating = 5, Comment = null, CreatedAt = orderLrm4.CreatedAt },
                new Review { OrderId = orderLrm5.Id, MerchantId = m_lareposteriademarta.Id, Rating = 4, Comment = "Muy rico, todo fresco.", CreatedAt = orderLrm5.CreatedAt }
            );
            db.SaveChanges();
        }

        if (!existingBusinessNames.Contains("Postres Bosque Negro"))
        {
            var m_postresbosquenegroUser = new User
            {
                Email        = "postresbosquenegro@resq.com",
                PasswordHash = hash,
                IsActive     = true,
                CreatedAt    = now,
                MerchantProfile = new MerchantProfile
                {
                    BusinessName       = "Postres Bosque Negro",
                    Cuit               = "30-78901234-4",
                    Address            = "Av. General Paz 150, Córdoba Centro",
                    Latitude           = -31.4147m,
                    Longitude          = -64.1836m,
                    ContactPhone       = "+54 351 555-7009",
                    MpConnectionStatus = MpConnectionStatus.Disconnected,
                    CreatedAt          = now
                },
                UserRoles = [new UserRole { Role = Role.Merchant, CreatedAt = now }]
            };
            db.Users.Add(m_postresbosquenegroUser);
            db.SaveChanges();

            var m_postresbosquenegro = m_postresbosquenegroUser.MerchantProfile!;
            m_postresbosquenegro.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/0/0a/Laika_ac_Shiseido_Parlour_%287635656008%29.jpg/960px-Laika_ac_Shiseido_Parlour_%287635656008%29.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_postresbosquenegro.Id, CategoryId = categories["Postres"] });

            var m_postresbosquenegro_p1 = new Product
            {
                MerchantId      = m_postresbosquenegro.Id,
                Name            = "Pack Sorpresa Dulce",
                Description     = "Selección sorpresa de postres de vitrina del día — texturas y sabores variados a mitad de precio.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 1500m,
                SalePrice       = 750m,
                StockQuantity   = 6,
                PickupTimeStart = new TimeOnly(19, 0),
                PickupTimeEnd   = new TimeOnly(21, 30),
                IsActive        = true,
                CreatedAt       = now
            };
            var m_postresbosquenegro_p2 = new Product
            {
                MerchantId      = m_postresbosquenegro.Id,
                Name            = "Pack Copa de Chocolate",
                Description     = "Copa individual de mousse de chocolate semiamargo con coulis de frutos rojos.",
                ProductType     = ProductType.ExplicitItem,
                OriginalPrice   = 1100m,
                SalePrice       = 550m,
                StockQuantity   = 8,
                PickupTimeStart = new TimeOnly(17, 0),
                PickupTimeEnd   = new TimeOnly(20, 30),
                IsActive        = true,
                CreatedAt       = now
            };
            db.Products.AddRange(m_postresbosquenegro_p1, m_postresbosquenegro_p2);
            db.SaveChanges();

            m_postresbosquenegro_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/c/cf/Chocolate_dessert%2C_Hetschbach.jpg/960px-Chocolate_dessert%2C_Hetschbach.jpg";
            m_postresbosquenegro_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/f/f6/Chocolate_mousse_-_stonesoup.jpg/960px-Chocolate_mousse_-_stonesoup.jpg";
            db.SaveChanges();

            // ─── Reviews ──────────────────────────────────────────────────────────
            var orderPbn1 = new Order { ConsumerId = demoConsumers[0].Id, MerchantId = m_postresbosquenegro.Id, TotalAmount = 750m, PlatformFee = 75m, MerchantEarnings = 675m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PBN-01", CreatedAt = now.AddDays(-5) };
            var orderPbn2 = new Order { ConsumerId = demoConsumers[3].Id, MerchantId = m_postresbosquenegro.Id, TotalAmount = 550m, PlatformFee = 55m, MerchantEarnings = 495m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PBN-02", CreatedAt = now.AddDays(-11) };
            var orderPbn3 = new Order { ConsumerId = demoConsumers[6].Id, MerchantId = m_postresbosquenegro.Id, TotalAmount = 750m, PlatformFee = 75m, MerchantEarnings = 675m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PBN-03", CreatedAt = now.AddDays(-19) };
            var orderPbn4 = new Order { ConsumerId = demoConsumers[1].Id, MerchantId = m_postresbosquenegro.Id, TotalAmount = 550m, PlatformFee = 55m, MerchantEarnings = 495m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PBN-04", CreatedAt = now.AddDays(-27) };
            var orderPbn5 = new Order { ConsumerId = demoConsumers[7].Id, MerchantId = m_postresbosquenegro.Id, TotalAmount = 750m, PlatformFee = 75m, MerchantEarnings = 675m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PBN-05", CreatedAt = now.AddDays(-35) };
            db.Orders.AddRange(orderPbn1, orderPbn2, orderPbn3, orderPbn4, orderPbn5);
            db.SaveChanges();

            db.OrderDetails.AddRange(
                new OrderDetail { OrderId = orderPbn1.Id, ProductId = m_postresbosquenegro_p1.Id, Quantity = 1, UnitPrice = 750m },
                new OrderDetail { OrderId = orderPbn2.Id, ProductId = m_postresbosquenegro_p2.Id, Quantity = 1, UnitPrice = 550m },
                new OrderDetail { OrderId = orderPbn3.Id, ProductId = m_postresbosquenegro_p1.Id, Quantity = 1, UnitPrice = 750m },
                new OrderDetail { OrderId = orderPbn4.Id, ProductId = m_postresbosquenegro_p2.Id, Quantity = 1, UnitPrice = 550m },
                new OrderDetail { OrderId = orderPbn5.Id, ProductId = m_postresbosquenegro_p1.Id, Quantity = 1, UnitPrice = 750m }
            );
            db.SaveChanges();

            db.Reviews.AddRange(
                new Review { OrderId = orderPbn1.Id, MerchantId = m_postresbosquenegro.Id, Rating = 5, Comment = "Excelente variedad de postres, todo fresco y bien presentado.", CreatedAt = orderPbn1.CreatedAt },
                new Review { OrderId = orderPbn2.Id, MerchantId = m_postresbosquenegro.Id, Rating = 4, Comment = "La copa de mousse riquísima, aunque un poco chica para el precio.", CreatedAt = orderPbn2.CreatedAt },
                new Review { OrderId = orderPbn3.Id, MerchantId = m_postresbosquenegro.Id, Rating = 5, Comment = null, CreatedAt = orderPbn3.CreatedAt },
                new Review { OrderId = orderPbn4.Id, MerchantId = m_postresbosquenegro.Id, Rating = 4, Comment = "Buenísimo el mousse de chocolate, ya pedí una segunda vez.", CreatedAt = orderPbn4.CreatedAt },
                new Review { OrderId = orderPbn5.Id, MerchantId = m_postresbosquenegro.Id, Rating = 2, Comment = "Llegué en el horario indicado y ya no quedaban postres, tuve que esperar sin mucha explicación.", CreatedAt = orderPbn5.CreatedAt }
            );
            db.SaveChanges();
        }

        if (!existingBusinessNames.Contains("Dulce Tentación"))
        {
            var m_dulcetentacionUser = new User
            {
                Email        = "dulcetentacion@resq.com",
                PasswordHash = hash,
                IsActive     = true,
                CreatedAt    = now,
                MerchantProfile = new MerchantProfile
                {
                    BusinessName       = "Dulce Tentación",
                    Cuit               = "30-79012345-5",
                    Address            = "Independencia 900, Güemes",
                    Latitude           = -31.4235m,
                    Longitude          = -64.1815m,
                    ContactPhone       = "+54 351 555-8001",
                    MpConnectionStatus = MpConnectionStatus.Disconnected,
                    CreatedAt          = now
                },
                UserRoles = [new UserRole { Role = Role.Merchant, CreatedAt = now }]
            };
            db.Users.Add(m_dulcetentacionUser);
            db.SaveChanges();

            var m_dulcetentacion = m_dulcetentacionUser.MerchantProfile!;
            m_dulcetentacion.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/c/cb/Interior_de_la_pasteler%C3%ADa_Oporto.jpg/960px-Interior_de_la_pasteler%C3%ADa_Oporto.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_dulcetentacion.Id, CategoryId = categories["Postres"] });

            var m_dulcetentacion_p1 = new Product
            {
                MerchantId      = m_dulcetentacion.Id,
                Name            = "Pack Sorpresa Tentación",
                Description     = "Selección sorpresa de postres de vitrina elegidos por la casa: tortas, tartas y bocaditos dulces del día.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 1400m,
                SalePrice       = 700m,
                StockQuantity   = 7,
                PickupTimeStart = new TimeOnly(18, 0),
                PickupTimeEnd   = new TimeOnly(21, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            var m_dulcetentacion_p2 = new Product
            {
                MerchantId      = m_dulcetentacion.Id,
                Name            = "Pack Flan Casero",
                Description     = "Flan casero de vainilla bañado en abundante caramelo, con un toque de dulce de leche.",
                ProductType     = ProductType.ExplicitItem,
                OriginalPrice   = 900m,
                SalePrice       = 450m,
                StockQuantity   = 10,
                PickupTimeStart = new TimeOnly(16, 30),
                PickupTimeEnd   = new TimeOnly(20, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            db.Products.AddRange(m_dulcetentacion_p1, m_dulcetentacion_p2);
            db.SaveChanges();

            m_dulcetentacion_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/f/fe/Chocolate_pralines_and_other_sweets.jpg/960px-Chocolate_pralines_and_other_sweets.jpg";
            m_dulcetentacion_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/0/06/Cr%C3%A8me_caramel_at_NB_Steak_JK.jpg/960px-Cr%C3%A8me_caramel_at_NB_Steak_JK.jpg";
            db.SaveChanges();

            // ─── Reviews ──────────────────────────────────────────────────────────
            var orderDt1 = new Order { ConsumerId = demoConsumers[2].Id, MerchantId = m_dulcetentacion.Id, TotalAmount = 450m, PlatformFee = 45m, MerchantEarnings = 405m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-DT-01", CreatedAt = now.AddDays(-7) };
            var orderDt2 = new Order { ConsumerId = demoConsumers[5].Id, MerchantId = m_dulcetentacion.Id, TotalAmount = 700m, PlatformFee = 70m, MerchantEarnings = 630m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-DT-02", CreatedAt = now.AddDays(-14) };
            var orderDt3 = new Order { ConsumerId = demoConsumers[0].Id, MerchantId = m_dulcetentacion.Id, TotalAmount = 700m, PlatformFee = 70m, MerchantEarnings = 630m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-DT-03", CreatedAt = now.AddDays(-23) };
            var orderDt4 = new Order { ConsumerId = demoConsumers[6].Id, MerchantId = m_dulcetentacion.Id, TotalAmount = 450m, PlatformFee = 45m, MerchantEarnings = 405m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-DT-04", CreatedAt = now.AddDays(-31) };
            var orderDt5 = new Order { ConsumerId = demoConsumers[3].Id, MerchantId = m_dulcetentacion.Id, TotalAmount = 700m, PlatformFee = 70m, MerchantEarnings = 630m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-DT-05", CreatedAt = now.AddDays(-44) };
            db.Orders.AddRange(orderDt1, orderDt2, orderDt3, orderDt4, orderDt5);
            db.SaveChanges();

            db.OrderDetails.AddRange(
                new OrderDetail { OrderId = orderDt1.Id, ProductId = m_dulcetentacion_p2.Id, Quantity = 1, UnitPrice = 450m },
                new OrderDetail { OrderId = orderDt2.Id, ProductId = m_dulcetentacion_p1.Id, Quantity = 1, UnitPrice = 700m },
                new OrderDetail { OrderId = orderDt3.Id, ProductId = m_dulcetentacion_p1.Id, Quantity = 1, UnitPrice = 700m },
                new OrderDetail { OrderId = orderDt4.Id, ProductId = m_dulcetentacion_p2.Id, Quantity = 1, UnitPrice = 450m },
                new OrderDetail { OrderId = orderDt5.Id, ProductId = m_dulcetentacion_p1.Id, Quantity = 1, UnitPrice = 700m }
            );
            db.SaveChanges();

            db.Reviews.AddRange(
                new Review { OrderId = orderDt1.Id, MerchantId = m_dulcetentacion.Id, Rating = 5, Comment = "El flan casero con caramelo es una locura, muy bien de punto.", CreatedAt = orderDt1.CreatedAt },
                new Review { OrderId = orderDt2.Id, MerchantId = m_dulcetentacion.Id, Rating = 4, Comment = "Buena selección de postres de vitrina, variada y bien armada.", CreatedAt = orderDt2.CreatedAt },
                new Review { OrderId = orderDt3.Id, MerchantId = m_dulcetentacion.Id, Rating = 3, Comment = "Estuvo bien, pero el pack sorpresa vino con menos variedad que la vez pasada.", CreatedAt = orderDt3.CreatedAt },
                new Review { OrderId = orderDt4.Id, MerchantId = m_dulcetentacion.Id, Rating = 5, Comment = null, CreatedAt = orderDt4.CreatedAt },
                new Review { OrderId = orderDt5.Id, MerchantId = m_dulcetentacion.Id, Rating = 4, Comment = "Todo fresco y a buen precio.", CreatedAt = orderDt5.CreatedAt }
            );
            db.SaveChanges();
        }

        if (!existingBusinessNames.Contains("Pizzería La Mezzaluna"))
        {
            var m_pizzerialamezzalunaUser = new User
            {
                Email        = "pizzerialamezzaluna@resq.com",
                PasswordHash = hash,
                IsActive     = true,
                CreatedAt    = now,
                MerchantProfile = new MerchantProfile
                {
                    BusinessName       = "Pizzería La Mezzaluna",
                    Cuit               = "30-80123456-6",
                    Address            = "Av. Duarte Quirós 3200, Alberdi",
                    Latitude           = -31.4155m,
                    Longitude          = -64.2145m,
                    ContactPhone       = "+54 351 555-8002",
                    MpConnectionStatus = MpConnectionStatus.Disconnected,
                    CreatedAt          = now
                },
                UserRoles = [new UserRole { Role = Role.Merchant, CreatedAt = now }]
            };
            db.Users.Add(m_pizzerialamezzalunaUser);
            db.SaveChanges();

            var m_pizzerialamezzaluna = m_pizzerialamezzalunaUser.MerchantProfile!;
            m_pizzerialamezzaluna.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/5/55/Ancora_Oven_Freret_New_Orleans.jpg/960px-Ancora_Oven_Freret_New_Orleans.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_pizzerialamezzaluna.Id, CategoryId = categories["Pizzería"] });

            var m_pizzerialamezzaluna_p1 = new Product
            {
                MerchantId      = m_pizzerialamezzaluna.Id,
                Name            = "Pack Sorpresa Mezzaluna",
                Description     = "Media pizza grande de la casa, con los sabores sorpresa que quedaron del día.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 2200m,
                SalePrice       = 1100m,
                StockQuantity   = 6,
                PickupTimeStart = new TimeOnly(20, 0),
                PickupTimeEnd   = new TimeOnly(23, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            var m_pizzerialamezzaluna_p2 = new Product
            {
                MerchantId      = m_pizzerialamezzaluna.Id,
                Name            = "Pack Pizza Muzzarella",
                Description     = "Pizza grande de muzzarella recién horneada con hojas de albahaca fresca.",
                ProductType     = ProductType.ExplicitItem,
                OriginalPrice   = 1800m,
                SalePrice       = 900m,
                StockQuantity   = 8,
                PickupTimeStart = new TimeOnly(19, 30),
                PickupTimeEnd   = new TimeOnly(22, 30),
                IsActive        = true,
                CreatedAt       = now
            };
            db.Products.AddRange(m_pizzerialamezzaluna_p1, m_pizzerialamezzaluna_p2);
            db.SaveChanges();

            m_pizzerialamezzaluna_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/a/a3/Eq_it-na_pizza-margherita_sep2005_sml.jpg/960px-Eq_it-na_pizza-margherita_sep2005_sml.jpg";
            m_pizzerialamezzaluna_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/7/7a/Margherita_Cl%C3%A1ssica_Italiana.jpg/960px-Margherita_Cl%C3%A1ssica_Italiana.jpg";
            db.SaveChanges();

            // ─── Reviews ──────────────────────────────────────────────────────────
            var orderPlm1 = new Order { ConsumerId = demoConsumers[4].Id, MerchantId = m_pizzerialamezzaluna.Id, TotalAmount = 900m, PlatformFee = 90m, MerchantEarnings = 810m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PLM-01", CreatedAt = now.AddDays(-6) };
            var orderPlm2 = new Order { ConsumerId = demoConsumers[1].Id, MerchantId = m_pizzerialamezzaluna.Id, TotalAmount = 1100m, PlatformFee = 110m, MerchantEarnings = 990m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PLM-02", CreatedAt = now.AddDays(-15) };
            var orderPlm3 = new Order { ConsumerId = demoConsumers[7].Id, MerchantId = m_pizzerialamezzaluna.Id, TotalAmount = 900m, PlatformFee = 90m, MerchantEarnings = 810m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PLM-03", CreatedAt = now.AddDays(-21) };
            var orderPlm4 = new Order { ConsumerId = demoConsumers[2].Id, MerchantId = m_pizzerialamezzaluna.Id, TotalAmount = 1100m, PlatformFee = 110m, MerchantEarnings = 990m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PLM-04", CreatedAt = now.AddDays(-29) };
            var orderPlm5 = new Order { ConsumerId = demoConsumers[5].Id, MerchantId = m_pizzerialamezzaluna.Id, TotalAmount = 900m, PlatformFee = 90m, MerchantEarnings = 810m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PLM-05", CreatedAt = now.AddDays(-38) };
            db.Orders.AddRange(orderPlm1, orderPlm2, orderPlm3, orderPlm4, orderPlm5);
            db.SaveChanges();

            db.OrderDetails.AddRange(
                new OrderDetail { OrderId = orderPlm1.Id, ProductId = m_pizzerialamezzaluna_p2.Id, Quantity = 1, UnitPrice = 900m },
                new OrderDetail { OrderId = orderPlm2.Id, ProductId = m_pizzerialamezzaluna_p1.Id, Quantity = 1, UnitPrice = 1100m },
                new OrderDetail { OrderId = orderPlm3.Id, ProductId = m_pizzerialamezzaluna_p2.Id, Quantity = 1, UnitPrice = 900m },
                new OrderDetail { OrderId = orderPlm4.Id, ProductId = m_pizzerialamezzaluna_p1.Id, Quantity = 1, UnitPrice = 1100m },
                new OrderDetail { OrderId = orderPlm5.Id, ProductId = m_pizzerialamezzaluna_p2.Id, Quantity = 1, UnitPrice = 900m }
            );
            db.SaveChanges();

            db.Reviews.AddRange(
                new Review { OrderId = orderPlm1.Id, MerchantId = m_pizzerialamezzaluna.Id, Rating = 5, Comment = "La muzzarella con albahaca fresca, recién salida del horno. Diez puntos.", CreatedAt = orderPlm1.CreatedAt },
                new Review { OrderId = orderPlm2.Id, MerchantId = m_pizzerialamezzaluna.Id, Rating = 4, Comment = "Buena la media pizza sorpresa, esta vez tocó una combinación de fugazzeta que no esperaba.", CreatedAt = orderPlm2.CreatedAt },
                new Review { OrderId = orderPlm3.Id, MerchantId = m_pizzerialamezzaluna.Id, Rating = 5, Comment = null, CreatedAt = orderPlm3.CreatedAt },
                new Review { OrderId = orderPlm4.Id, MerchantId = m_pizzerialamezzaluna.Id, Rating = 3, Comment = "Rica, pero tardaron bastante en entregar el pedido dentro del horario de retiro.", CreatedAt = orderPlm4.CreatedAt },
                new Review { OrderId = orderPlm5.Id, MerchantId = m_pizzerialamezzaluna.Id, Rating = 4, Comment = "Muy buena relación precio-calidad.", CreatedAt = orderPlm5.CreatedAt }
            );
            db.SaveChanges();
        }

        if (!existingBusinessNames.Contains("Pizzería Don Vito"))
        {
            var m_pizzeriadonvitoUser = new User
            {
                Email        = "pizzeriadonvito@resq.com",
                PasswordHash = hash,
                IsActive     = true,
                CreatedAt    = now,
                MerchantProfile = new MerchantProfile
                {
                    BusinessName       = "Pizzería Don Vito",
                    Cuit               = "30-81234567-7",
                    Address            = "Bv. Illia 780, Nueva Córdoba",
                    Latitude           = -31.4278m,
                    Longitude          = -64.1875m,
                    ContactPhone       = "+54 351 555-8003",
                    MpConnectionStatus = MpConnectionStatus.Disconnected,
                    CreatedAt          = now
                },
                UserRoles = [new UserRole { Role = Role.Merchant, CreatedAt = now }]
            };
            db.Users.Add(m_pizzeriadonvitoUser);
            db.SaveChanges();

            var m_pizzeriadonvito = m_pizzeriadonvitoUser.MerchantProfile!;
            m_pizzeriadonvito.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/8/8c/Cotogna_pizza_oven.jpg/960px-Cotogna_pizza_oven.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_pizzeriadonvito.Id, CategoryId = categories["Pizzería"] });

            var m_pizzeriadonvito_p1 = new Product
            {
                MerchantId      = m_pizzeriadonvito.Id,
                Name            = "Pack Sorpresa Don Vito",
                Description     = "Pizza grande con los sabores del día — variedad sorpresa según lo que quedó en el mostrador.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 2000m,
                SalePrice       = 1000m,
                StockQuantity   = 5,
                PickupTimeStart = new TimeOnly(20, 30),
                PickupTimeEnd   = new TimeOnly(23, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            var m_pizzeriadonvito_p2 = new Product
            {
                MerchantId      = m_pizzeriadonvito.Id,
                Name            = "Pack Fugazzeta",
                Description     = "Fugazzeta rellena de muzzarella y cubierta con abundante cebolla caramelizada.",
                ProductType     = ProductType.ExplicitItem,
                OriginalPrice   = 1900m,
                SalePrice       = 950m,
                StockQuantity   = 6,
                PickupTimeStart = new TimeOnly(19, 0),
                PickupTimeEnd   = new TimeOnly(22, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            db.Products.AddRange(m_pizzeriadonvito_p1, m_pizzeriadonvito_p2);
            db.SaveChanges();

            m_pizzeriadonvito_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/9b/Cheese_crust_pizza.jpg/960px-Cheese_crust_pizza.jpg";
            m_pizzeriadonvito_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/d/d7/Fugazzetta_en_pizzeria_Guerrin%2C_Buenos_Aires_%28detalle%29.jpg/960px-Fugazzetta_en_pizzeria_Guerrin%2C_Buenos_Aires_%28detalle%29.jpg";
            db.SaveChanges();

            // ─── Reviews ──────────────────────────────────────────────────────────
            var orderPdv1 = new Order { ConsumerId = demoConsumers[3].Id, MerchantId = m_pizzeriadonvito.Id, TotalAmount = 950m, PlatformFee = 95m, MerchantEarnings = 855m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PDV-01", CreatedAt = now.AddDays(-8) };
            var orderPdv2 = new Order { ConsumerId = demoConsumers[6].Id, MerchantId = m_pizzeriadonvito.Id, TotalAmount = 1000m, PlatformFee = 100m, MerchantEarnings = 900m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PDV-02", CreatedAt = now.AddDays(-16) };
            var orderPdv3 = new Order { ConsumerId = demoConsumers[0].Id, MerchantId = m_pizzeriadonvito.Id, TotalAmount = 950m, PlatformFee = 95m, MerchantEarnings = 855m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PDV-03", CreatedAt = now.AddDays(-24) };
            var orderPdv4 = new Order { ConsumerId = demoConsumers[4].Id, MerchantId = m_pizzeriadonvito.Id, TotalAmount = 1000m, PlatformFee = 100m, MerchantEarnings = 900m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PDV-04", CreatedAt = now.AddDays(-33) };
            var orderPdv5 = new Order { ConsumerId = demoConsumers[2].Id, MerchantId = m_pizzeriadonvito.Id, TotalAmount = 950m, PlatformFee = 95m, MerchantEarnings = 855m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PDV-05", CreatedAt = now.AddDays(-45) };
            db.Orders.AddRange(orderPdv1, orderPdv2, orderPdv3, orderPdv4, orderPdv5);
            db.SaveChanges();

            db.OrderDetails.AddRange(
                new OrderDetail { OrderId = orderPdv1.Id, ProductId = m_pizzeriadonvito_p2.Id, Quantity = 1, UnitPrice = 950m },
                new OrderDetail { OrderId = orderPdv2.Id, ProductId = m_pizzeriadonvito_p1.Id, Quantity = 1, UnitPrice = 1000m },
                new OrderDetail { OrderId = orderPdv3.Id, ProductId = m_pizzeriadonvito_p2.Id, Quantity = 1, UnitPrice = 950m },
                new OrderDetail { OrderId = orderPdv4.Id, ProductId = m_pizzeriadonvito_p1.Id, Quantity = 1, UnitPrice = 1000m },
                new OrderDetail { OrderId = orderPdv5.Id, ProductId = m_pizzeriadonvito_p2.Id, Quantity = 1, UnitPrice = 950m }
            );
            db.SaveChanges();

            db.Reviews.AddRange(
                new Review { OrderId = orderPdv1.Id, MerchantId = m_pizzeriadonvito.Id, Rating = 5, Comment = "La fugazzeta con la cebolla bien caramelizada, una masa espectacular.", CreatedAt = orderPdv1.CreatedAt },
                new Review { OrderId = orderPdv2.Id, MerchantId = m_pizzeriadonvito.Id, Rating = 4, Comment = "Buena pizza sorpresa, esta vez tocó una napolitana bien cargada.", CreatedAt = orderPdv2.CreatedAt },
                new Review { OrderId = orderPdv3.Id, MerchantId = m_pizzeriadonvito.Id, Rating = 5, Comment = null, CreatedAt = orderPdv3.CreatedAt },
                new Review { OrderId = orderPdv4.Id, MerchantId = m_pizzeriadonvito.Id, Rating = 3, Comment = "Buena pizza, aunque el pack sorpresa vino más chico de lo que parecía en las fotos.", CreatedAt = orderPdv4.CreatedAt },
                new Review { OrderId = orderPdv5.Id, MerchantId = m_pizzeriadonvito.Id, Rating = 4, Comment = "Siempre efectiva, la pido seguido.", CreatedAt = orderPdv5.CreatedAt }
            );
            db.SaveChanges();
        }

        if (!existingBusinessNames.Contains("Parrilla El Fogón"))
        {
            var m_parrillaelfogonUser = new User
            {
                Email        = "parrillaelfogon@resq.com",
                PasswordHash = hash,
                IsActive     = true,
                CreatedAt    = now,
                MerchantProfile = new MerchantProfile
                {
                    BusinessName       = "Parrilla El Fogón",
                    Cuit               = "30-82345678-8",
                    Address            = "Av. Sabattini 1450, Alta Córdoba",
                    Latitude           = -31.3899m,
                    Longitude          = -64.1795m,
                    ContactPhone       = "+54 351 555-8004",
                    MpConnectionStatus = MpConnectionStatus.Disconnected,
                    CreatedAt          = now
                },
                UserRoles = [new UserRole { Role = Role.Merchant, CreatedAt = now }]
            };
            db.Users.Add(m_parrillaelfogonUser);
            db.SaveChanges();

            var m_parrillaelfogon = m_parrillaelfogonUser.MerchantProfile!;
            m_parrillaelfogon.PhotoUrl = "https://commons.wikimedia.org/wiki/Special:FilePath/Interior%20dining%20room%20of%20a%20LongHorn%20Steakhouse%20restaurant%20in%20Blairsville%2C%20Georgia%2003.jpg?width=960";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_parrillaelfogon.Id, CategoryId = categories["Parrilla"] });

            var m_parrillaelfogon_p1 = new Product
            {
                MerchantId      = m_parrillaelfogon.Id,
                Name            = "Pack Sorpresa Parrillero",
                Description     = "Selección sorpresa de cortes y achuras de nuestra parrilla, lista para el cierre del día.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 3000m,
                SalePrice       = 1500m,
                StockQuantity   = 4,
                PickupTimeStart = new TimeOnly(21, 0),
                PickupTimeEnd   = new TimeOnly(23, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            var m_parrillaelfogon_p2 = new Product
            {
                MerchantId      = m_parrillaelfogon.Id,
                Name            = "Pack Choripán Doble",
                Description     = "Dos choripanes jugosos con chimichurri casero, recién salidos de la parrilla.",
                ProductType     = ProductType.ExplicitItem,
                OriginalPrice   = 1400m,
                SalePrice       = 700m,
                StockQuantity   = 8,
                PickupTimeStart = new TimeOnly(20, 0),
                PickupTimeEnd   = new TimeOnly(23, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            db.Products.AddRange(m_parrillaelfogon_p1, m_parrillaelfogon_p2);
            db.SaveChanges();

            m_parrillaelfogon_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/3/3e/Bife_de_chorizo_a_punto_02.jpg/960px-Bife_de_chorizo_a_punto_02.jpg";
            m_parrillaelfogon_p2.ImageUrl = "https://commons.wikimedia.org/wiki/Special:FilePath/Sausages_rolls_chimichurri_sauces.jpg";
            db.SaveChanges();

            var orderPef1 = new Order { ConsumerId = demoConsumers[0].Id, MerchantId = m_parrillaelfogon.Id, TotalAmount = 1500m, PlatformFee = 150m, MerchantEarnings = 1350m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PEF-01", CreatedAt = now.AddDays(-6)  };
            var orderPef2 = new Order { ConsumerId = demoConsumers[3].Id, MerchantId = m_parrillaelfogon.Id, TotalAmount = 700m,  PlatformFee = 70m,  MerchantEarnings = 630m,  ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PEF-02", CreatedAt = now.AddDays(-12) };
            var orderPef3 = new Order { ConsumerId = demoConsumers[5].Id, MerchantId = m_parrillaelfogon.Id, TotalAmount = 1500m, PlatformFee = 150m, MerchantEarnings = 1350m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PEF-03", CreatedAt = now.AddDays(-20) };
            var orderPef4 = new Order { ConsumerId = demoConsumers[1].Id, MerchantId = m_parrillaelfogon.Id, TotalAmount = 700m,  PlatformFee = 70m,  MerchantEarnings = 630m,  ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PEF-04", CreatedAt = now.AddDays(-28) };
            var orderPef5 = new Order { ConsumerId = demoConsumers[7].Id, MerchantId = m_parrillaelfogon.Id, TotalAmount = 1500m, PlatformFee = 150m, MerchantEarnings = 1350m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-PEF-05", CreatedAt = now.AddDays(-35) };
            db.Orders.AddRange(orderPef1, orderPef2, orderPef3, orderPef4, orderPef5);
            db.SaveChanges();

            db.OrderDetails.AddRange(
                new OrderDetail { OrderId = orderPef1.Id, ProductId = m_parrillaelfogon_p1.Id, Quantity = 1, UnitPrice = 1500m },
                new OrderDetail { OrderId = orderPef2.Id, ProductId = m_parrillaelfogon_p2.Id, Quantity = 1, UnitPrice = 700m },
                new OrderDetail { OrderId = orderPef3.Id, ProductId = m_parrillaelfogon_p1.Id, Quantity = 1, UnitPrice = 1500m },
                new OrderDetail { OrderId = orderPef4.Id, ProductId = m_parrillaelfogon_p2.Id, Quantity = 1, UnitPrice = 700m },
                new OrderDetail { OrderId = orderPef5.Id, ProductId = m_parrillaelfogon_p1.Id, Quantity = 1, UnitPrice = 1500m }
            );
            db.SaveChanges();

            db.Reviews.AddRange(
                new Review { OrderId = orderPef1.Id, MerchantId = m_parrillaelfogon.Id, Rating = 5, Comment = "Impecable el pack sorpresa, vino con bife de chorizo y unas achuras riquísimas. Vale cada peso.", CreatedAt = now.AddDays(-6)  },
                new Review { OrderId = orderPef2.Id, MerchantId = m_parrillaelfogon.Id, Rating = 4, Comment = "Muy rico el choripán, buen chimichurri casero.", CreatedAt = now.AddDays(-12) },
                new Review { OrderId = orderPef3.Id, MerchantId = m_parrillaelfogon.Id, Rating = 3, Comment = "Llegué un poco tarde y ya no quedaban achuras, solo carne. Igual estaba buena.", CreatedAt = now.AddDays(-20) },
                new Review { OrderId = orderPef4.Id, MerchantId = m_parrillaelfogon.Id, Rating = 5, Comment = null, CreatedAt = now.AddDays(-28) },
                new Review { OrderId = orderPef5.Id, MerchantId = m_parrillaelfogon.Id, Rating = 5, Comment = "Excelente relación precio-calidad, la carne estaba en su punto.", CreatedAt = now.AddDays(-35) }
            );
            db.SaveChanges();
        }

        if (!existingBusinessNames.Contains("Asador Criollo"))
        {
            var m_asadorcriolloUser = new User
            {
                Email        = "asadorcriollo@resq.com",
                PasswordHash = hash,
                IsActive     = true,
                CreatedAt    = now,
                MerchantProfile = new MerchantProfile
                {
                    BusinessName       = "Asador Criollo",
                    Cuit               = "30-83456789-9",
                    Address            = "Av. Circunvalación 2200, Villa Belgrano",
                    Latitude           = -31.3782m,
                    Longitude          = -64.2288m,
                    ContactPhone       = "+54 351 555-8005",
                    MpConnectionStatus = MpConnectionStatus.Disconnected,
                    CreatedAt          = now
                },
                UserRoles = [new UserRole { Role = Role.Merchant, CreatedAt = now }]
            };
            db.Users.Add(m_asadorcriolloUser);
            db.SaveChanges();

            var m_asadorcriollo = m_asadorcriolloUser.MerchantProfile!;
            m_asadorcriollo.PhotoUrl = "https://commons.wikimedia.org/wiki/Special:FilePath/Restaurante%20Clemente.jpg?width=960";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_asadorcriollo.Id, CategoryId = categories["Parrilla"] });

            var m_asadorcriollo_p1 = new Product
            {
                MerchantId      = m_asadorcriollo.Id,
                Name            = "Pack Sorpresa Criollo",
                Description     = "Selección sorpresa de cortes de asado y achuras del día, acompañados con guarniciones caseras.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 2800m,
                SalePrice       = 1400m,
                StockQuantity   = 5,
                PickupTimeStart = new TimeOnly(21, 0),
                PickupTimeEnd   = new TimeOnly(23, 30),
                IsActive        = true,
                CreatedAt       = now
            };
            var m_asadorcriollo_p2 = new Product
            {
                MerchantId      = m_asadorcriollo.Id,
                Name            = "Pack Provoleta & Pan",
                Description     = "Provoleta derretida a la parrilla, servida con pan casero recién horneado.",
                ProductType     = ProductType.ExplicitItem,
                OriginalPrice   = 1300m,
                SalePrice       = 650m,
                StockQuantity   = 7,
                PickupTimeStart = new TimeOnly(20, 0),
                PickupTimeEnd   = new TimeOnly(22, 30),
                IsActive        = true,
                CreatedAt       = now
            };
            db.Products.AddRange(m_asadorcriollo_p1, m_asadorcriollo_p2);
            db.SaveChanges();

            m_asadorcriollo_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/d/d7/Grilled_meat_rolls_served_on_a_black_plate.jpg/960px-Grilled_meat_rolls_served_on_a_black_plate.jpg";
            m_asadorcriollo_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/3/30/Grilled_Halloumi.jpg/960px-Grilled_Halloumi.jpg";
            db.SaveChanges();

            var orderAc1 = new Order { ConsumerId = demoConsumers[2].Id, MerchantId = m_asadorcriollo.Id, TotalAmount = 1400m, PlatformFee = 140m, MerchantEarnings = 1260m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-AC-01", CreatedAt = now.AddDays(-7)  };
            var orderAc2 = new Order { ConsumerId = demoConsumers[4].Id, MerchantId = m_asadorcriollo.Id, TotalAmount = 650m,  PlatformFee = 65m,  MerchantEarnings = 585m,  ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-AC-02", CreatedAt = now.AddDays(-15) };
            var orderAc3 = new Order { ConsumerId = demoConsumers[6].Id, MerchantId = m_asadorcriollo.Id, TotalAmount = 1400m, PlatformFee = 140m, MerchantEarnings = 1260m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-AC-03", CreatedAt = now.AddDays(-22) };
            var orderAc4 = new Order { ConsumerId = demoConsumers[0].Id, MerchantId = m_asadorcriollo.Id, TotalAmount = 650m,  PlatformFee = 65m,  MerchantEarnings = 585m,  ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-AC-04", CreatedAt = now.AddDays(-30) };
            var orderAc5 = new Order { ConsumerId = demoConsumers[3].Id, MerchantId = m_asadorcriollo.Id, TotalAmount = 1400m, PlatformFee = 140m, MerchantEarnings = 1260m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-AC-05", CreatedAt = now.AddDays(-40) };
            db.Orders.AddRange(orderAc1, orderAc2, orderAc3, orderAc4, orderAc5);
            db.SaveChanges();

            db.OrderDetails.AddRange(
                new OrderDetail { OrderId = orderAc1.Id, ProductId = m_asadorcriollo_p1.Id, Quantity = 1, UnitPrice = 1400m },
                new OrderDetail { OrderId = orderAc2.Id, ProductId = m_asadorcriollo_p2.Id, Quantity = 1, UnitPrice = 650m },
                new OrderDetail { OrderId = orderAc3.Id, ProductId = m_asadorcriollo_p1.Id, Quantity = 1, UnitPrice = 1400m },
                new OrderDetail { OrderId = orderAc4.Id, ProductId = m_asadorcriollo_p2.Id, Quantity = 1, UnitPrice = 650m },
                new OrderDetail { OrderId = orderAc5.Id, ProductId = m_asadorcriollo_p1.Id, Quantity = 1, UnitPrice = 1400m }
            );
            db.SaveChanges();

            db.Reviews.AddRange(
                new Review { OrderId = orderAc1.Id, MerchantId = m_asadorcriollo.Id, Rating = 5, Comment = "Un golazo el pack sorpresa, mucha cantidad de carne y muy sabrosa.", CreatedAt = now.AddDays(-7)  },
                new Review { OrderId = orderAc2.Id, MerchantId = m_asadorcriollo.Id, Rating = 4, Comment = "La provoleta estaba muy buena, el pan casero un plus.", CreatedAt = now.AddDays(-15) },
                new Review { OrderId = orderAc3.Id, MerchantId = m_asadorcriollo.Id, Rating = 2, Comment = "El pack vino más chico de lo que esperaba para el precio original.", CreatedAt = now.AddDays(-22) },
                new Review { OrderId = orderAc4.Id, MerchantId = m_asadorcriollo.Id, Rating = 5, Comment = null, CreatedAt = now.AddDays(-30) },
                new Review { OrderId = orderAc5.Id, MerchantId = m_asadorcriollo.Id, Rating = 4, Comment = "Todo fresco y bien cocido, repetiría sin dudas.", CreatedAt = now.AddDays(-40) }
            );
            db.SaveChanges();
        }

        if (!existingBusinessNames.Contains("Supermercado La Esquina"))
        {
            var m_superlaesquinaUser = new User
            {
                Email        = "superlaesquina@resq.com",
                PasswordHash = hash,
                IsActive     = true,
                CreatedAt    = now,
                MerchantProfile = new MerchantProfile
                {
                    BusinessName       = "Supermercado La Esquina",
                    Cuit               = "30-84567890-0",
                    Address            = "Av. Colón 4200, Cerro de las Rosas",
                    Latitude           = -31.3897m,
                    Longitude          = -64.2312m,
                    ContactPhone       = "+54 351 555-9001",
                    MpConnectionStatus = MpConnectionStatus.Disconnected,
                    CreatedAt          = now
                },
                UserRoles = [new UserRole { Role = Role.Merchant, CreatedAt = now }]
            };
            db.Users.Add(m_superlaesquinaUser);
            db.SaveChanges();

            var m_superlaesquina = m_superlaesquinaUser.MerchantProfile!;
            m_superlaesquina.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/c/c3/The_interior_of_a_Fresh_Market_store.jpg/960px-The_interior_of_a_Fresh_Market_store.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_superlaesquina.Id, CategoryId = categories["Supermercado"] });

            var m_superlaesquina_p1 = new Product
            {
                MerchantId      = m_superlaesquina.Id,
                Name            = "Pack Verdulería Sorpresa",
                Description     = "Selección sorpresa de frutas y verduras frescas, en perfecto estado y a un precio increíble antes de salir de góndola.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 2500m,
                SalePrice       = 1000m,
                StockQuantity   = 10,
                PickupTimeStart = new TimeOnly(20, 30),
                PickupTimeEnd   = new TimeOnly(22, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            var m_superlaesquina_p2 = new Product
            {
                MerchantId      = m_superlaesquina.Id,
                Name            = "Pack Almacén del Día",
                Description     = "Selección sorpresa de productos de almacén en perfecto estado, ideal para renovar la despensa a un precio increíble.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 3000m,
                SalePrice       = 1500m,
                StockQuantity   = 4,
                PickupTimeStart = new TimeOnly(21, 0),
                PickupTimeEnd   = new TimeOnly(22, 30),
                IsActive        = true,
                CreatedAt       = now
            };
            db.Products.AddRange(m_superlaesquina_p1, m_superlaesquina_p2);
            db.SaveChanges();

            m_superlaesquina_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/d/de/Crate_of_fruit_and_vegetables.jpg/960px-Crate_of_fruit_and_vegetables.jpg";
            m_superlaesquina_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/e/e2/Glass_jars_filled_with_various_grains_and_pasta_on_a_wooden_shelf.jpg/960px-Glass_jars_filled_with_various_grains_and_pasta_on_a_wooden_shelf.jpg";
            db.SaveChanges();

            var orderSle1 = new Order { ConsumerId = demoConsumers[1].Id, MerchantId = m_superlaesquina.Id, TotalAmount = 1000m, PlatformFee = 100m, MerchantEarnings = 900m,  ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-SLE-01", CreatedAt = now.AddDays(-5)  };
            var orderSle2 = new Order { ConsumerId = demoConsumers[5].Id, MerchantId = m_superlaesquina.Id, TotalAmount = 1500m, PlatformFee = 150m, MerchantEarnings = 1350m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-SLE-02", CreatedAt = now.AddDays(-14) };
            var orderSle3 = new Order { ConsumerId = demoConsumers[7].Id, MerchantId = m_superlaesquina.Id, TotalAmount = 1000m, PlatformFee = 100m, MerchantEarnings = 900m,  ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-SLE-03", CreatedAt = now.AddDays(-25) };
            var orderSle4 = new Order { ConsumerId = demoConsumers[2].Id, MerchantId = m_superlaesquina.Id, TotalAmount = 1500m, PlatformFee = 150m, MerchantEarnings = 1350m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-SLE-04", CreatedAt = now.AddDays(-33) };
            var orderSle5 = new Order { ConsumerId = demoConsumers[6].Id, MerchantId = m_superlaesquina.Id, TotalAmount = 1000m, PlatformFee = 100m, MerchantEarnings = 900m,  ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-SLE-05", CreatedAt = now.AddDays(-42) };
            var orderSle6 = new Order { ConsumerId = demoConsumers[0].Id, MerchantId = m_superlaesquina.Id, TotalAmount = 1500m, PlatformFee = 150m, MerchantEarnings = 1350m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-SLE-06", CreatedAt = now.AddDays(-9)  };
            db.Orders.AddRange(orderSle1, orderSle2, orderSle3, orderSle4, orderSle5, orderSle6);
            db.SaveChanges();

            db.OrderDetails.AddRange(
                new OrderDetail { OrderId = orderSle1.Id, ProductId = m_superlaesquina_p1.Id, Quantity = 1, UnitPrice = 1000m },
                new OrderDetail { OrderId = orderSle2.Id, ProductId = m_superlaesquina_p2.Id, Quantity = 1, UnitPrice = 1500m },
                new OrderDetail { OrderId = orderSle3.Id, ProductId = m_superlaesquina_p1.Id, Quantity = 1, UnitPrice = 1000m },
                new OrderDetail { OrderId = orderSle4.Id, ProductId = m_superlaesquina_p2.Id, Quantity = 1, UnitPrice = 1500m },
                new OrderDetail { OrderId = orderSle5.Id, ProductId = m_superlaesquina_p1.Id, Quantity = 1, UnitPrice = 1000m },
                new OrderDetail { OrderId = orderSle6.Id, ProductId = m_superlaesquina_p2.Id, Quantity = 1, UnitPrice = 1500m }
            );
            db.SaveChanges();

            db.Reviews.AddRange(
                new Review { OrderId = orderSle1.Id, MerchantId = m_superlaesquina.Id, Rating = 5, Comment = "Excelente el pack de verdulería, todo fresco y variado.", CreatedAt = now.AddDays(-5)  },
                new Review { OrderId = orderSle2.Id, MerchantId = m_superlaesquina.Id, Rating = 4, Comment = "Buenos productos de almacén, algunos ya los tenía pero sirven igual.", CreatedAt = now.AddDays(-14) },
                new Review { OrderId = orderSle3.Id, MerchantId = m_superlaesquina.Id, Rating = 3, Comment = "Había poca variedad de verduras ese día, pero estaban en buen estado.", CreatedAt = now.AddDays(-25) },
                new Review { OrderId = orderSle4.Id, MerchantId = m_superlaesquina.Id, Rating = 5, Comment = null, CreatedAt = now.AddDays(-33) },
                new Review { OrderId = orderSle5.Id, MerchantId = m_superlaesquina.Id, Rating = 4, Comment = "Muy rico, todo fresco.", CreatedAt = now.AddDays(-42) },
                new Review { OrderId = orderSle6.Id, MerchantId = m_superlaesquina.Id, Rating = 5, Comment = "Me sorprendió la cantidad de productos por ese precio, súper recomendable.", CreatedAt = now.AddDays(-9) }
            );
            db.SaveChanges();
        }

        if (!existingBusinessNames.Contains("Mercado Fresco Sur"))
        {
            var m_mercadofrescosurUser = new User
            {
                Email        = "mercadofrescosur@resq.com",
                PasswordHash = hash,
                IsActive     = true,
                CreatedAt    = now,
                MerchantProfile = new MerchantProfile
                {
                    BusinessName       = "Mercado Fresco Sur",
                    Cuit               = "30-85678901-1",
                    Address            = "Bv. Guzmán 1100, Alberdi",
                    Latitude           = -31.4041m,
                    Longitude          = -64.1998m,
                    ContactPhone       = "+54 351 555-9002",
                    MpConnectionStatus = MpConnectionStatus.Disconnected,
                    CreatedAt          = now
                },
                UserRoles = [new UserRole { Role = Role.Merchant, CreatedAt = now }]
            };
            db.Users.Add(m_mercadofrescosurUser);
            db.SaveChanges();

            var m_mercadofrescosur = m_mercadofrescosurUser.MerchantProfile!;
            m_mercadofrescosur.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/f/f4/DFC_2101_Vivid_purple_round_eggplants_piled_with_fresh_green_beans_and_cucumbers_at_a_market_stall.jpg/960px-DFC_2101_Vivid_purple_round_eggplants_piled_with_fresh_green_beans_and_cucumbers_at_a_market_stall.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_mercadofrescosur.Id, CategoryId = categories["Supermercado"] });

            var m_mercadofrescosur_p1 = new Product
            {
                MerchantId      = m_mercadofrescosur.Id,
                Name            = "Pack Frutas y Verduras Frescas",
                Description     = "Excedente sorpresa de la verdulería: frutas y verduras frescas de estación, ideales para consumir en los próximos días.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 2200m,
                SalePrice       = 900m,
                StockQuantity   = 12,
                PickupTimeStart = new TimeOnly(19, 30),
                PickupTimeEnd   = new TimeOnly(21, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            var m_mercadofrescosur_p2 = new Product
            {
                MerchantId      = m_mercadofrescosur.Id,
                Name            = "Pack Lácteos del Día",
                Description     = "Selección sorpresa de productos lácteos frescos, mantenidos en cadena de frío hasta el momento del retiro.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 2800m,
                SalePrice       = 1300m,
                StockQuantity   = 1,
                PickupTimeStart = new TimeOnly(20, 0),
                PickupTimeEnd   = new TimeOnly(21, 30),
                IsActive        = true,
                CreatedAt       = now
            };
            db.Products.AddRange(m_mercadofrescosur_p1, m_mercadofrescosur_p2);
            db.SaveChanges();

            m_mercadofrescosur_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/f/ff/Fresh_red_peppers_in_a_wooden_crate.jpg/960px-Fresh_red_peppers_in_a_wooden_crate.jpg";
            m_mercadofrescosur_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/3/35/DairyProductsGermany.jpg/960px-DairyProductsGermany.jpg";
            db.SaveChanges();

            var orderMfs1 = new Order { ConsumerId = demoConsumers[3].Id, MerchantId = m_mercadofrescosur.Id, TotalAmount = 900m,  PlatformFee = 90m,  MerchantEarnings = 810m,  ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-MFS-01", CreatedAt = now.AddDays(-8)  };
            var orderMfs2 = new Order { ConsumerId = demoConsumers[4].Id, MerchantId = m_mercadofrescosur.Id, TotalAmount = 1300m, PlatformFee = 130m, MerchantEarnings = 1170m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-MFS-02", CreatedAt = now.AddDays(-18) };
            var orderMfs3 = new Order { ConsumerId = demoConsumers[6].Id, MerchantId = m_mercadofrescosur.Id, TotalAmount = 900m,  PlatformFee = 90m,  MerchantEarnings = 810m,  ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-MFS-03", CreatedAt = now.AddDays(-27) };
            var orderMfs4 = new Order { ConsumerId = demoConsumers[1].Id, MerchantId = m_mercadofrescosur.Id, TotalAmount = 1300m, PlatformFee = 130m, MerchantEarnings = 1170m, ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-MFS-04", CreatedAt = now.AddDays(-36) };
            var orderMfs5 = new Order { ConsumerId = demoConsumers[7].Id, MerchantId = m_mercadofrescosur.Id, TotalAmount = 900m,  PlatformFee = 90m,  MerchantEarnings = 810m,  ExternalReference = Guid.NewGuid().ToString(), OrderStatus = OrderStatus.PickedUp, PickupCode = "RSQ-MFS-05", CreatedAt = now.AddDays(-44) };
            db.Orders.AddRange(orderMfs1, orderMfs2, orderMfs3, orderMfs4, orderMfs5);
            db.SaveChanges();

            db.OrderDetails.AddRange(
                new OrderDetail { OrderId = orderMfs1.Id, ProductId = m_mercadofrescosur_p1.Id, Quantity = 1, UnitPrice = 900m },
                new OrderDetail { OrderId = orderMfs2.Id, ProductId = m_mercadofrescosur_p2.Id, Quantity = 1, UnitPrice = 1300m },
                new OrderDetail { OrderId = orderMfs3.Id, ProductId = m_mercadofrescosur_p1.Id, Quantity = 1, UnitPrice = 900m },
                new OrderDetail { OrderId = orderMfs4.Id, ProductId = m_mercadofrescosur_p2.Id, Quantity = 1, UnitPrice = 1300m },
                new OrderDetail { OrderId = orderMfs5.Id, ProductId = m_mercadofrescosur_p1.Id, Quantity = 1, UnitPrice = 900m }
            );
            db.SaveChanges();

            db.Reviews.AddRange(
                new Review { OrderId = orderMfs1.Id, MerchantId = m_mercadofrescosur.Id, Rating = 5, Comment = "Las verduras llegaron re frescas, ni se notaba que eran excedente.", CreatedAt = now.AddDays(-8)  },
                new Review { OrderId = orderMfs2.Id, MerchantId = m_mercadofrescosur.Id, Rating = 4, Comment = "Los lácteos estaban perfectos, bien de frío.", CreatedAt = now.AddDays(-18) },
                new Review { OrderId = orderMfs3.Id, MerchantId = m_mercadofrescosur.Id, Rating = 3, Comment = "Justo se había agotado el pack cuando llegué y me dieron uno más chico.", CreatedAt = now.AddDays(-27) },
                new Review { OrderId = orderMfs4.Id, MerchantId = m_mercadofrescosur.Id, Rating = 5, Comment = null, CreatedAt = now.AddDays(-36) },
                new Review { OrderId = orderMfs5.Id, MerchantId = m_mercadofrescosur.Id, Rating = 4, Comment = "Buena variedad de frutas y verduras, todo en buen estado.", CreatedAt = now.AddDays(-44) }
            );
            db.SaveChanges();
        }

    }

    public static void Seed(ResQDbContext db)
    {
        // Idempotent guard — skip if already seeded
        if (db.Categories.Any()) return;

        var now = DateTime.UtcNow;

        // BCrypt work factor 8 is intentionally lower than production (12) to keep seeding fast
        var hash = BCrypt.Net.BCrypt.HashPassword("ResQ1234!", 8);

        // ─── 1. Categories ────────────────────────────────────────────────────────
        var catPanaderia   = new Category { Name = "Panadería",    CreatedAt = now };
        var catSushi       = new Category { Name = "Sushi",        CreatedAt = now };
        var catCafe        = new Category { Name = "Rosticería",   CreatedAt = now };
        var catRestaurante = new Category { Name = "Restaurante",  CreatedAt = now };
        var catVegano      = new Category { Name = "Vegano",       CreatedAt = now };
        var catHeladeria   = new Category { Name = "Fiambrería",   CreatedAt = now };

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
            Email        = "rosticeriadonapola@resq.com",
            PasswordHash = hash,
            IsActive     = true,
            CreatedAt    = now,
            MerchantProfile = new MerchantProfile
            {
                BusinessName       = "Rosticería Doña Pola",
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

        // External image URLs (not MinIO-relative paths) — these seeded merchants never go
        // through the real upload flow, so there's no actual file for ResolvePublicUrl to
        // resolve. A hardcoded "http://localhost/..." here breaks on any other host, since
        // ResolvePublicUrl leaves already-absolute URLs untouched (see its doc comment).
        panaderia.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/2/2d/Boulangerie_Bardoulet_%28Contrevoz%29_-_int%C3%A9rieur_de_la_boutique.jpg/960px-Boulangerie_Bardoulet_%28Contrevoz%29_-_int%C3%A9rieur_de_la_boutique.jpg";
        sushi.PhotoUrl     = "https://upload.wikimedia.org/wikipedia/commons/thumb/3/3a/HK_Central_MTR_Station_shop_%E6%9D%BF%E9%95%B7%E5%A3%BD%E5%8F%B8_Itacho_Sushi_restaurant_interior_visitors_Jan-2012.jpg/960px-HK_Central_MTR_Station_shop_%E6%9D%BF%E9%95%B7%E5%A3%BD%E5%8F%B8_Itacho_Sushi_restaurant_interior_visitors_Jan-2012.jpg";
        cafe.PhotoUrl      = "https://commons.wikimedia.org/wiki/Special:FilePath/Pollo%20asado%203.jpg?width=960";

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
            Name            = "Pack Tarta del Día",
            Description     = "Tarta de jamón y queso recién horneada, porción generosa lista para retirar.",
            ProductType     = ProductType.ExplicitItem,
            OriginalPrice   = 900m,
            SalePrice       = 450m,
            StockQuantity   = 10,
            PickupTimeStart = new TimeOnly(18, 0),
            PickupTimeEnd   = new TimeOnly(20, 30),
            IsActive        = true,
            CreatedAt       = now
        };

        var packMerienda = new Product
        {
            MerchantId      = cafe.Id,
            Name            = "Pack Pizza al Molde",
            Description     = "Porción de pizza al molde con muzzarella, jamón y aceitunas, ideal para la cena.",
            ProductType     = ProductType.ExplicitItem,
            OriginalPrice   = 1200m,
            SalePrice       = 600m,
            StockQuantity   = 7,
            PickupTimeStart = new TimeOnly(19, 0),
            PickupTimeEnd   = new TimeOnly(21, 30),
            IsActive        = true,
            CreatedAt       = now
        };

        db.Products.AddRange(packSorpresaDulce, packMedialunas, packMixtoSushi, packRolls, packCafe, packMerienda);
        db.SaveChanges();

        packSorpresaDulce.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/8/8f/Bakery_-_Free_For_Commercial_Use_-_FFCU_%2826777902185%29.jpg/960px-Bakery_-_Free_For_Commercial_Use_-_FFCU_%2826777902185%29.jpg";
        packMedialunas.ImageUrl    = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/95/Medialunas_argentinas.jpg/960px-Medialunas_argentinas.jpg";
        packMixtoSushi.ImageUrl    = "https://upload.wikimedia.org/wikipedia/commons/thumb/e/e6/Homemade_sushi_rolls%2C_2009.jpg/960px-Homemade_sushi_rolls%2C_2009.jpg";
        packRolls.ImageUrl         = "https://commons.wikimedia.org/wiki/Special:FilePath/Sunny_Sushi_rainbow_roll.JPG";
        packCafe.ImageUrl          = "https://upload.wikimedia.org/wikipedia/commons/thumb/1/11/Ham_%26_Cheese_Quiche.jpg/960px-Ham_%26_Cheese_Quiche.jpg";
        packMerienda.ImageUrl      = "https://upload.wikimedia.org/wikipedia/commons/thumb/7/77/Top-down_view_of_a_complete_Argentine_pizza_with_mozzarella%2C_ham%2C_onion%2C_and_olives_on_a_round_wooden_board.jpg/960px-Top-down_view_of_a_complete_Argentine_pizza_with_mozzarella%2C_ham%2C_onion%2C_and_olives_on_a_round_wooden_board.jpg";

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
        // Rosticería Doña Pola: 2 reviews → avg 4.5
        db.Reviews.AddRange(
            new Review { OrderId = order2.Id,  MerchantId = sushi.Id,     Rating = 5, Comment = "El sushi estaba increíble, muy fresco y abundante. 100% recomendado!",          CreatedAt = now.AddDays(-1)  },
            new Review { OrderId = orderH1.Id, MerchantId = sushi.Id,     Rating = 5, Comment = "Excelente presentación y frescura. Los rolls son espectaculares.",              CreatedAt = now.AddDays(-7)  },
            new Review { OrderId = orderH2.Id, MerchantId = sushi.Id,     Rating = 4, Comment = "Muy buen sushi, variado y fresco. El precio es una ganga total.",               CreatedAt = now.AddDays(-14) },
            new Review { OrderId = orderH3.Id, MerchantId = panaderia.Id, Rating = 5, Comment = "Pack sorpresa espectacular, muy variado y todo recién horneado. Ahorro enorme!", CreatedAt = now.AddDays(-10) },
            new Review { OrderId = orderH4.Id, MerchantId = panaderia.Id, Rating = 4, Comment = "Muy buenas medialunas, bastante frescas. El precio es excelente.",              CreatedAt = now.AddDays(-17) },
            new Review { OrderId = orderH5.Id, MerchantId = panaderia.Id, Rating = 4, Comment = "Buena relación precio/calidad. Las facturas del día estaban muy ricas.",        CreatedAt = now.AddDays(-24) },
            new Review { OrderId = orderH6.Id, MerchantId = cafe.Id,      Rating = 5, Comment = "La pizza al molde estaba buenísima, bien de queso. La volvería a pedir sin dudas.", CreatedAt = now.AddDays(-5)  },
            new Review { OrderId = orderH7.Id, MerchantId = cafe.Id,      Rating = 4, Comment = "Tarta de jamón y queso muy rica y abundante. Todo fresco.",                       CreatedAt = now.AddDays(-12) }
        );
        db.SaveChanges();
    }
}
