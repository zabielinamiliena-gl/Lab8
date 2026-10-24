using System;
using System.Collections.Generic;

namespace Variant1_OrderProcessing
{
    public class Customer
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public string Email { get; private set; }
        public string Type { get; private set; }
        public decimal TotalSpent { get; private set; }

        public decimal DiscountRate
        {
            get
            {
                if (Type == "VIP")
                    return 0.15m;
                if (Type == "Regular")
                    return 0.05m;
                return 0m;
            }
        }

        private Customer(int id, string name, string email, string type, decimal totalSpent)
        {
            Id = id;
            Name = name;
            Email = email;
            Type = type;
            TotalSpent = totalSpent;
        }

        public static Customer Create(int id, string name, string email, string type, decimal totalSpent)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Ім'я клієнта не може бути порожнім.");
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email не може бути порожнім.");
            if (type != "New" && type != "Regular" && type != "VIP")
                throw new ArgumentException("Тип клієнта має бути New, Regular або VIP.");
            if (totalSpent < 0)
                throw new ArgumentException("Накопичена сума не може бути від'ємною.");

            return new Customer(id, name, email, type, totalSpent);
        }
    }

    public class Product
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public decimal Price { get; private set; }
        public string Category { get; private set; }

        private Product(int id, string name, decimal price, string category)
        {
            Id = id;
            Name = name;
            Price = price;
            Category = category;
        }

        public static Product Create(int id, string name, decimal price, string category)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Назва товару не може бути порожньою.");
            if (price < 0)
                throw new ArgumentException("Ціна не може бути від'ємною.");

            return new Product(id, name, price, category);
        }
    }

    public class OrderItem
    {
        public Product Product { get; private set; }
        public int Quantity { get; private set; }
        public decimal LineTotal => Product.Price * Quantity;

        private OrderItem(Product product, int quantity)
        {
            Product = product;
            Quantity = quantity;
        }

        public static OrderItem Create(Product product, int quantity)
        {
            if (product == null)
                throw new ArgumentException("Товар позиції не може бути порожнім.");
            if (quantity <= 0)
                throw new ArgumentException("Кількість має бути більшою за нуль.");

            return new OrderItem(product, quantity);
        }
    }

    public class Order
    {
        private readonly List<OrderItem> items;

        public int Id { get; private set; }
        public Customer Customer { get; private set; }
        public IReadOnlyCollection<OrderItem> Items => items.AsReadOnly();
        public decimal TotalAmount => SumLines();
        public decimal DiscountAmount => TotalAmount * Customer.DiscountRate;
        public decimal TaxAmount => (TotalAmount - DiscountAmount) * 0.20m;
        public decimal FinalAmount => TotalAmount - DiscountAmount + TaxAmount;
        public string Status { get; private set; }

        private Order(int id, Customer customer, List<OrderItem> createdItems)
        {
            Id = id;
            Customer = customer;
            items = new List<OrderItem>(createdItems);
            Status = "Processed";
        }

        public static Order Create(int id, Customer customer, params (Product Product, int Quantity)[] lines)
        {
            if (customer == null)
                throw new ArgumentException("Клієнт не може бути порожнім.");
            if (lines == null || lines.Length == 0)
                throw new ArgumentException("Замовлення не може бути порожнім.");

            List<OrderItem> createdItems = new List<OrderItem>();
            foreach ((Product Product, int Quantity) line in lines)
                createdItems.Add(OrderItem.Create(line.Product, line.Quantity));

            return new Order(id, customer, createdItems);
        }

        private decimal SumLines()
        {
            decimal sum = 0m;
            foreach (OrderItem item in items)
                sum += item.LineTotal;
            return sum;
        }
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

            Customer customer = Customer.Create(1, name, "client@example.com", customerType, 15000m);
            Product laptop = Product.Create(1, "Ноутбук", 30000m, "Electronics");
            Product mouse = Product.Create(2, "Мишка", 800m, "Accessories");
            Order order = Order.Create(101, customer, (laptop, 1), (mouse, 2));

            Console.WriteLine(
                $"Замовлення #{order.Id} для {order.Customer.Name}\n" +
                $"Сума: {order.TotalAmount} грн\n" +
                $"Знижка: {order.DiscountAmount} грн\n" +
                $"ПДВ: {order.TaxAmount} грн\n" +
                $"До сплати: {order.FinalAmount} грн");
            Console.WriteLine($"[БД] Замовлення #{order.Id} збережено у файл orders.txt.");
            Console.WriteLine($"[Email] Повідомлення відправлено на {order.Customer.Email}");
        }
    }

    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            new LegacyOrderProcessor().ProcessConsoleOrder();
        }
    }
}
