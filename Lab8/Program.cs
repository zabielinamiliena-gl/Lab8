using static System.Runtime.InteropServices.JavaScript.JSType;

< variants language = "C#" >
< variant number = "1" >
< domain_description > Інтернет - магазин електроніки та побутової техніки.</domain_description>
<code_purpose>Обробка замовлення клієнта — перевірка коректності даних, розрахунок загальної вартості з урахуванням накопичувальної знижки та ПДВ, формування текстового рахунку, збереження замовлення у файл та відправка email-сповіщення.</code_purpose>
<code>
<![CDATA[ namespace Variant1_OrderProcessing
{
    // Опис предметної області: Інтернет-магазин електроніки та побутової техніки.
    // Завдання коду: Обробка замовлення клієнта — перевірка коректності даних, розрахунок загальної вартості з урахуванням накопичувальної знижки та ПДВ, формування текстового рахунку, збереження замовлення у файл та відправка email-сповіщення.

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Type { get; set; } // "Regular", "VIP", "New"
        public decimal TotalSpent { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
    }

    public class OrderItem
    {
        public Product Product { get; set; }
        public int Quantity { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public Customer Customer { get; set; }
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string Status { get; set; }
    }

    public class LegacyOrderProcessor
    {
        public void ProcessConsoleOrder()
        {
            Console.WriteLine("=== Оформлення замовлення ===");
            Console.Write("Введіть ім'я клієнта: ");
            string name = Console.ReadLine();
            Console.Write("Введіть тип клієнта (New/Regular/VIP): ");
            string customerType = Console.ReadLine();

            Customer customer = new Customer { Id = 1, Name = name, Email = "client@example.com", Type = customerType, TotalSpent = 15000m };
            Order order = new Order { Id = 101, Customer = customer, Status = "New" };

            Product p1 = new Product { Id = 1, Name = "Ноутбук", Price = 30000m, Category = "Electronics" };
            Product p2 = new Product { Id = 2, Name = "Мишка", Price = 800m, Category = "Accessories" };

            order.Items.Add(new OrderItem { Product = p1, Quantity = 1 });
            order.Items.Add(new OrderItem { Product = p2, Quantity = 2 });

            decimal subtotal = 0m;
            foreach (var item in order.Items)
            {
                subtotal += item.Product.Price * item.Quantity;
            }
            order.TotalAmount = subtotal;

            decimal discountRate = 0m;
            if (order.Customer.Type == "VIP")
            {
                discountRate = 0.15m;
            }
            else if (order.Customer.Type == "Regular")
            {
                discountRate = 0.05m;
            }
            else if (order.Customer.Type == "New")
            {
                discountRate = 0.0m;
            }
            order.DiscountAmount = order.TotalAmount * discountRate;

            order.TaxAmount = (order.TotalAmount - order.DiscountAmount) * 0.20m;
            order.FinalAmount = order.TotalAmount - order.DiscountAmount + order.TaxAmount;
            order.Status = "Processed";

            string receipt = $"Замовлення #{order.Id} для {order.Customer.Name}\nСума: {order.TotalAmount} грн\nЗнижка: {order.DiscountAmount} грн\nПДВ: {order.TaxAmount} грн\nДо сплати: {order.FinalAmount} грн";
            Console.WriteLine(receipt);

            Console.WriteLine($"[БД] Замовлення #{order.Id} збережено у файл orders.txt.");
            Console.WriteLine($"[Email] Повідомлення відправлено на {order.Customer.Email}");
        }
    }
} ]]>
