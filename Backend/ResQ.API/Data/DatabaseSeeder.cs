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
            "Panadería", "Sushi", "Café", "Restaurante", "Vegano", "Heladería",
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
            m_panaderiasanmartin.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/8/8a/Bread_rolls_at_a_bakery.jpg/960px-Bread_rolls_at_a_bakery.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_panaderiasanmartin.Id, CategoryId = categories["Panadería"] });

            var m_panaderiasanmartin_p1 = new Product
            {
                MerchantId      = m_panaderiasanmartin.Id,
                Name            = "Pack Sorpresa Panadero",
                Description     = "Selección de panes del día: baguettes, pan de campo y pan lactal recién horneado.",
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
                Description     = "Docena de facturas surtidas: vigilantes, cañoncitos y sacramentos.",
                ProductType     = ProductType.SurprisePack,
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

            m_panaderiasanmartin_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/f/f3/EasterSaris12Slovakia10.JPG/960px-EasterSaris12Slovakia10.JPG";
            m_panaderiasanmartin_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/b/b7/Facturas_en_plato.jpg/960px-Facturas_en_plato.jpg";
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
            m_sushiyamato.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/3/3a/HK_Central_MTR_Station_shop_%E6%9D%BF%E9%95%B7%E5%A3%BD%E5%8F%B8_Itacho_Sushi_restaurant_interior_visitors_Jan-2012.jpg/960px-HK_Central_MTR_Station_shop_%E6%9D%BF%E9%95%B7%E5%A3%BD%E5%8F%B8_Itacho_Sushi_restaurant_interior_visitors_Jan-2012.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_sushiyamato.Id, CategoryId = categories["Sushi"] });

            var m_sushiyamato_p1 = new Product
            {
                MerchantId      = m_sushiyamato.Id,
                Name            = "Pack Yamato Mixto",
                Description     = "24 piezas variadas: niguiris, rolls y sashimi del día.",
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
                Description     = "6 temakis surtidos preparados con pescados frescos del día.",
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
        }

        if (!existingBusinessNames.Contains("Café del Boulevard"))
        {
            var m_cafedelboulevardUser = new User
            {
                Email        = "cafedelboulevard@resq.com",
                PasswordHash = hash,
                IsActive     = true,
                CreatedAt    = now,
                MerchantProfile = new MerchantProfile
                {
                    BusinessName       = "Café del Boulevard",
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
            m_cafedelboulevard.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/f/fb/Coffee_shop_1_-_Wellington%2C_New_Zealand.jpg/960px-Coffee_shop_1_-_Wellington%2C_New_Zealand.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_cafedelboulevard.Id, CategoryId = categories["Café"] });

            var m_cafedelboulevard_p1 = new Product
            {
                MerchantId      = m_cafedelboulevard.Id,
                Name            = "Pack Desayuno Boulevard",
                Description     = "Café de especialidad + croissant + jugo de naranja exprimido.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 1000m,
                SalePrice       = 500m,
                StockQuantity   = 8,
                PickupTimeStart = new TimeOnly(8, 0),
                PickupTimeEnd   = new TimeOnly(11, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            var m_cafedelboulevard_p2 = new Product
            {
                MerchantId      = m_cafedelboulevard.Id,
                Name            = "Pack Merienda del Boulevard",
                Description     = "Té o café + porción de torta casera del día.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 950m,
                SalePrice       = 470m,
                StockQuantity   = 6,
                PickupTimeStart = new TimeOnly(17, 0),
                PickupTimeEnd   = new TimeOnly(19, 30),
                IsActive        = true,
                CreatedAt       = now
            };
            db.Products.AddRange(m_cafedelboulevard_p1, m_cafedelboulevard_p2);
            db.SaveChanges();

            m_cafedelboulevard_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/94/Breakfast_spread_with_coffee%2C_pastry%2C_and_juice_on_a_table_in_a_cozy_morning_setting.jpg/960px-Breakfast_spread_with_coffee%2C_pastry%2C_and_juice_on_a_table_in_a_cozy_morning_setting.jpg";
            m_cafedelboulevard_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/e/ef/Piece_of_chocolate_cake_on_a_white_plate_decorated_with_chocolate_sauce.jpg/960px-Piece_of_chocolate_cake_on_a_white_plate_decorated_with_chocolate_sauce.jpg";
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
                Description     = "Plato principal sorpresa según lo que quede del servicio del día.",
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
                Description     = "Entrada + plato principal del menú ejecutivo del día.",
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
                Description     = "Selección de platos del día a punto de finalizar el servicio.",
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
                Description     = "Entrada, plato principal y postre del menú del día.",
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
            m_verdevidavegano.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/8/8c/Juice_center.jpg/960px-Juice_center.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_verdevidavegano.Id, CategoryId = categories["Vegano"] });

            var m_verdevidavegano_p1 = new Product
            {
                MerchantId      = m_verdevidavegano.Id,
                Name            = "Pack Bowl Sorpresa",
                Description     = "Bowl de vegetales de estación, legumbres y salsas caseras.",
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
                ProductType     = ProductType.SurprisePack,
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
            m_raizcocinavegana.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/6/65/DZ3_0604_A_colorful_self-serve_salad_bar_with_fresh_greens_chopped_vegetables_and_toppings_neatly_arranged_in_stainless_steel_trays.jpg/960px-DZ3_0604_A_colorful_self-serve_salad_bar_with_fresh_greens_chopped_vegetables_and_toppings_neatly_arranged_in_stainless_steel_trays.jpg";

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
                ProductType     = ProductType.SurprisePack,
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
            m_raizcocinavegana_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/f/f3/Acai_bowl%2C_fresh_fruit_%2842965870152%29.jpg/960px-Acai_bowl%2C_fresh_fruit_%2842965870152%29.jpg";
            db.SaveChanges();
        }

        if (!existingBusinessNames.Contains("Heladería Cremolatti"))
        {
            var m_heladeriacremolattiUser = new User
            {
                Email        = "heladeriacremolatti@resq.com",
                PasswordHash = hash,
                IsActive     = true,
                CreatedAt    = now,
                MerchantProfile = new MerchantProfile
                {
                    BusinessName       = "Heladería Cremolatti",
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
            m_heladeriacremolatti.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/d/d6/Gelato_ice_cream.jpg/960px-Gelato_ice_cream.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_heladeriacremolatti.Id, CategoryId = categories["Heladería"] });

            var m_heladeriacremolatti_p1 = new Product
            {
                MerchantId      = m_heladeriacremolatti.Id,
                Name            = "Pack Sorpresa 1/2 Kg",
                Description     = "Medio kilo de helado artesanal, sabores a elección del heladero.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 3500m,
                SalePrice       = 1750m,
                StockQuantity   = 6,
                PickupTimeStart = new TimeOnly(18, 0),
                PickupTimeEnd   = new TimeOnly(22, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            var m_heladeriacremolatti_p2 = new Product
            {
                MerchantId      = m_heladeriacremolatti.Id,
                Name            = "Pack Postre Helado",
                Description     = "Copa de helado con topping sorpresa del día.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 1200m,
                SalePrice       = 600m,
                StockQuantity   = 10,
                PickupTimeStart = new TimeOnly(17, 0),
                PickupTimeEnd   = new TimeOnly(21, 30),
                IsActive        = true,
                CreatedAt       = now
            };
            db.Products.AddRange(m_heladeriacremolatti_p1, m_heladeriacremolatti_p2);
            db.SaveChanges();

            m_heladeriacremolatti_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/c/cb/Polaris_ice_cream_-_Dulce_de_leche%2C_vanilla_and_strawberry_-_%28Posadas%2C_Misiones%2C_Argentina%29.jpg/960px-Polaris_ice_cream_-_Dulce_de_leche%2C_vanilla_and_strawberry_-_%28Posadas%2C_Misiones%2C_Argentina%29.jpg";
            m_heladeriacremolatti_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/0/00/Dark_chocolate_chip_ice_cream_cone_-_August_2025_-_Sarah_Stierch.jpg/960px-Dark_chocolate_chip_ice_cream_cone_-_August_2025_-_Sarah_Stierch.jpg";
            db.SaveChanges();
        }

        if (!existingBusinessNames.Contains("Gelato del Sol"))
        {
            var m_gelatodelsolUser = new User
            {
                Email        = "gelatodelsol@resq.com",
                PasswordHash = hash,
                IsActive     = true,
                CreatedAt    = now,
                MerchantProfile = new MerchantProfile
                {
                    BusinessName       = "Gelato del Sol",
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
            m_gelatodelsol.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/0/0d/Gelato_artigianale_italiano%2C_Bertinelli.jpg/960px-Gelato_artigianale_italiano%2C_Bertinelli.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_gelatodelsol.Id, CategoryId = categories["Heladería"] });

            var m_gelatodelsol_p1 = new Product
            {
                MerchantId      = m_gelatodelsol.Id,
                Name            = "Pack Gelato Sorpresa",
                Description     = "1/4 kg de gelato artesanal, sabores variados del día.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 1800m,
                SalePrice       = 900m,
                StockQuantity   = 8,
                PickupTimeStart = new TimeOnly(17, 30),
                PickupTimeEnd   = new TimeOnly(21, 30),
                IsActive        = true,
                CreatedAt       = now
            };
            var m_gelatodelsol_p2 = new Product
            {
                MerchantId      = m_gelatodelsol.Id,
                Name            = "Pack Cucurucho Doble",
                Description     = "Cucurucho con dos sabores a elección del heladero.",
                ProductType     = ProductType.SurprisePack,
                OriginalPrice   = 900m,
                SalePrice       = 450m,
                StockQuantity   = 12,
                PickupTimeStart = new TimeOnly(16, 0),
                PickupTimeEnd   = new TimeOnly(20, 0),
                IsActive        = true,
                CreatedAt       = now
            };
            db.Products.AddRange(m_gelatodelsol_p1, m_gelatodelsol_p2);
            db.SaveChanges();

            m_gelatodelsol_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/3/33/Coppette_gelato.jpg/960px-Coppette_gelato.jpg";
            m_gelatodelsol_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/4/4b/Ice_Cream_Dessert_%28Unsplash%29.jpg/960px-Ice_Cream_Dessert_%28Unsplash%29.jpg";
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
            m_pasteleriadulcetrigo.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/3/3a/Pastry_assortment.jpg/960px-Pastry_assortment.jpg";

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
                ProductType     = ProductType.SurprisePack,
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

            m_pasteleriadulcetrigo_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/4/47/Bolo_Fiada.jpg/960px-Bolo_Fiada.jpg";
            m_pasteleriadulcetrigo_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/9/92/Pastelitos_criollos_argentinos.jpg";
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
            m_lareposteriademarta.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/0/03/Sweets%21.jpg/960px-Sweets%21.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_lareposteriademarta.Id, CategoryId = categories["Pastelería"] });

            var m_lareposteriademarta_p1 = new Product
            {
                MerchantId      = m_lareposteriademarta.Id,
                Name            = "Pack Sorpresa de Marta",
                Description     = "Porciones de tortas caseras a punto de finalizar el día.",
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
                Description     = "Media docena de alfajores artesanales surtidos.",
                ProductType     = ProductType.SurprisePack,
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

            m_lareposteriademarta_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/99/%D0%A7%D0%BE%D0%BA%D0%BE%D0%BB%D0%B0%D0%B4%D0%BD%D0%B0_%D0%BA%D0%BE%D1%86%D0%BA%D0%B0_%2C_%D0%BD%D0%B0_%D1%88%D0%B0%D1%80%D0%B5%D0%BD%D0%B0_%D0%BC%D0%B0%D1%81%D0%B0.jpg/960px-%D0%A7%D0%BE%D0%BA%D0%BE%D0%BB%D0%B0%D0%B4%D0%BD%D0%B0_%D0%BA%D0%BE%D1%86%D0%BA%D0%B0_%2C_%D0%BD%D0%B0_%D1%88%D0%B0%D1%80%D0%B5%D0%BD%D0%B0_%D0%BC%D0%B0%D1%81%D0%B0.jpg";
            m_lareposteriademarta_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/4/47/Alfajores_argentinos_-_2025.jpg/960px-Alfajores_argentinos_-_2025.jpg";
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
                Description     = "Selección de postres de vitrina a punto de finalizar el día.",
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
                Description     = "Copa de mousse de chocolate con frutos rojos.",
                ProductType     = ProductType.SurprisePack,
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
            m_dulcetentacion.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/8/8b/DFC_4073_Assorted_fruit_tarts_piled_high_-_fresh_strawberries_blueberries_grapes_and_kiwi_on_creamy_pastry_shells.jpg/960px-DFC_4073_Assorted_fruit_tarts_piled_high_-_fresh_strawberries_blueberries_grapes_and_kiwi_on_creamy_pastry_shells.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_dulcetentacion.Id, CategoryId = categories["Postres"] });

            var m_dulcetentacion_p1 = new Product
            {
                MerchantId      = m_dulcetentacion.Id,
                Name            = "Pack Sorpresa Tentación",
                Description     = "Postres variados de vitrina seleccionados por la casa.",
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
                Description     = "Flan casero con dulce de leche y crema.",
                ProductType     = ProductType.SurprisePack,
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
            m_dulcetentacion_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/4/43/Homemade_Flan.jpg/960px-Homemade_Flan.jpg";
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
                Description     = "Media pizza grande de sabores sorpresa del día.",
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
                Description     = "Pizza grande de muzzarella recién horneada.",
                ProductType     = ProductType.SurprisePack,
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
            m_pizzerialamezzaluna_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/8/89/Whole_Foods_Kitchen_Margherita_Pizza_2_%2815411931231%29.jpg/960px-Whole_Foods_Kitchen_Margherita_Pizza_2_%2815411931231%29.jpg";
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
            m_pizzeriadonvito.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/a/ae/Pizza_in_oven.jpg/960px-Pizza_in_oven.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_pizzeriadonvito.Id, CategoryId = categories["Pizzería"] });

            var m_pizzeriadonvito_p1 = new Product
            {
                MerchantId      = m_pizzeriadonvito.Id,
                Name            = "Pack Sorpresa Don Vito",
                Description     = "Pizza grande de sabores variados según lo disponible del día.",
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
                Description     = "Fugazzeta rellena con cebolla caramelizada.",
                ProductType     = ProductType.SurprisePack,
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
            m_pizzeriadonvito_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/7/7e/Whole_Argentine_pizza_al_molde_with_ham%2C_onion_and_olives.jpg/960px-Whole_Argentine_pizza_al_molde_with_ham%2C_onion_and_olives.jpg";
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
            m_parrillaelfogon.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/2/22/Asado_tradicional_argentino%2C_le%C3%B1a%2C_brasas.jpg/960px-Asado_tradicional_argentino%2C_le%C3%B1a%2C_brasas.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_parrillaelfogon.Id, CategoryId = categories["Parrilla"] });

            var m_parrillaelfogon_p1 = new Product
            {
                MerchantId      = m_parrillaelfogon.Id,
                Name            = "Pack Sorpresa Parrillero",
                Description     = "Selección de cortes y achuras a punto de finalizar el servicio.",
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
                Description     = "Dos choripanes con chimichurri casero.",
                ProductType     = ProductType.SurprisePack,
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

            m_parrillaelfogon_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/2/21/Plato_de_parrillada_argentina_con_costilla_de_carne%2C_chorizo%2C_costilla_de_cerdo_y_guarniciones.jpg/960px-Plato_de_parrillada_argentina_con_costilla_de_carne%2C_chorizo%2C_costilla_de_cerdo_y_guarniciones.jpg";
            m_parrillaelfogon_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/0/0c/Chorip%C3%A1n_%28argentine_sandwich_with_chorizo%29.jpg/960px-Chorip%C3%A1n_%28argentine_sandwich_with_chorizo%29.jpg";
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
            m_asadorcriollo.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/0/0c/TERRIBLE_PARRILLADA_ARGENTINA_-_Flickr_-_dr_pablogonzalez.jpg/960px-TERRIBLE_PARRILLADA_ARGENTINA_-_Flickr_-_dr_pablogonzalez.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_asadorcriollo.Id, CategoryId = categories["Parrilla"] });

            var m_asadorcriollo_p1 = new Product
            {
                MerchantId      = m_asadorcriollo.Id,
                Name            = "Pack Sorpresa Criollo",
                Description     = "Cortes de asado sorpresa del día con guarniciones.",
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
                Description     = "Provoleta a la parrilla con pan casero.",
                ProductType     = ProductType.SurprisePack,
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
            m_asadorcriollo_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/93/Provoleta_argentina.jpg/960px-Provoleta_argentina.jpg";
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
            m_superlaesquina.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/0/0d/Fruit_section_of_a_grocery_store.jpg/960px-Fruit_section_of_a_grocery_store.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_superlaesquina.Id, CategoryId = categories["Supermercado"] });

            var m_superlaesquina_p1 = new Product
            {
                MerchantId      = m_superlaesquina.Id,
                Name            = "Pack Verdulería Sorpresa",
                Description     = "Selección sorpresa de frutas y verduras del día, en perfecto estado pero cerca de la fecha de reposición.",
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
                Description     = "Fideos, salsas y productos de almacén próximos a vencer, aún dentro de la fecha de consumo.",
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
            m_superlaesquina_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/4/41/Supermarket_shelves.jpg/960px-Supermarket_shelves.jpg";
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
            m_mercadofrescosur.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/3/3c/Hilera_de_supermercado.jpg/960px-Hilera_de_supermercado.jpg";

            db.MerchantCategories.Add(new MerchantCategory { MerchantId = m_mercadofrescosur.Id, CategoryId = categories["Supermercado"] });

            var m_mercadofrescosur_p1 = new Product
            {
                MerchantId      = m_mercadofrescosur.Id,
                Name            = "Pack Frutas y Verduras Frescas",
                Description     = "Excedente de la verdulería: frutas y verduras frescas seleccionadas, ideales para consumir en los próximos días.",
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
                Description     = "Leche, yogures y quesos próximos a la fecha de vencimiento sugerida, en cadena de frío hasta el retiro.",
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

            m_mercadofrescosur_p1.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/6/64/Fruits_and_vegetables_at_market.jpg/960px-Fruits_and_vegetables_at_market.jpg";
            m_mercadofrescosur_p2.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/d/da/Milk_Aisle.jpg/960px-Milk_Aisle.jpg";
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

        // External image URLs (not MinIO-relative paths) — these seeded merchants never go
        // through the real upload flow, so there's no actual file for ResolvePublicUrl to
        // resolve. A hardcoded "http://localhost/..." here breaks on any other host, since
        // ResolvePublicUrl leaves already-absolute URLs untouched (see its doc comment).
        panaderia.PhotoUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/91/Sourdough_miche_%26_boule.jpg/960px-Sourdough_miche_%26_boule.jpg";
        sushi.PhotoUrl     = "https://upload.wikimedia.org/wikipedia/commons/thumb/3/3a/HK_Central_MTR_Station_shop_%E6%9D%BF%E9%95%B7%E5%A3%BD%E5%8F%B8_Itacho_Sushi_restaurant_interior_visitors_Jan-2012.jpg/960px-HK_Central_MTR_Station_shop_%E6%9D%BF%E9%95%B7%E5%A3%BD%E5%8F%B8_Itacho_Sushi_restaurant_interior_visitors_Jan-2012.jpg";
        cafe.PhotoUrl      = "https://upload.wikimedia.org/wikipedia/commons/thumb/f/fb/Coffee_shop_1_-_Wellington%2C_New_Zealand.jpg/960px-Coffee_shop_1_-_Wellington%2C_New_Zealand.jpg";

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

        packSorpresaDulce.ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/f/f3/EasterSaris12Slovakia10.JPG/960px-EasterSaris12Slovakia10.JPG";
        packMedialunas.ImageUrl    = "https://commons.wikimedia.org/wiki/Special:FilePath/Croissant.jpg";
        packMixtoSushi.ImageUrl    = "https://upload.wikimedia.org/wikipedia/commons/thumb/e/e6/Homemade_sushi_rolls%2C_2009.jpg/960px-Homemade_sushi_rolls%2C_2009.jpg";
        packRolls.ImageUrl         = "https://commons.wikimedia.org/wiki/Special:FilePath/Sunny_Sushi_rainbow_roll.JPG";
        packCafe.ImageUrl          = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/94/Breakfast_spread_with_coffee%2C_pastry%2C_and_juice_on_a_table_in_a_cozy_morning_setting.jpg/960px-Breakfast_spread_with_coffee%2C_pastry%2C_and_juice_on_a_table_in_a_cozy_morning_setting.jpg";
        packMerienda.ImageUrl      = "https://upload.wikimedia.org/wikipedia/commons/thumb/0/0c/Jam_and_toast.jpg/960px-Jam_and_toast.jpg";

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
