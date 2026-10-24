using System;
using System.Collections.Generic;
using System.IO;

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

    public class PlaceOrderRequest
    {
        public string Name { get; }
        public string CustomerType { get; }
        public int LaptopQuantity { get; }
        public int MouseQuantity { get; }

        public PlaceOrderRequest(string name, string customerType, int laptopQuantity, int mouseQuantity)
        {
            Name = name ?? "";
            CustomerType = customerType ?? "";
            LaptopQuantity = laptopQuantity;
            MouseQuantity = mouseQuantity;
        }
    }

    public class OrderResult
    {
        public int Id { get; }
        public string CustomerName { get; }
        public decimal TotalAmount { get; }
        public decimal DiscountAmount { get; }
        public decimal TaxAmount { get; }
        public decimal FinalAmount { get; }
        public string Email { get; }
        public string SavedFile { get; }

        public OrderResult(Order order, string savedFile)
        {
            Id = order.Id;
            CustomerName = order.Customer.Name;
            TotalAmount = order.TotalAmount;
            DiscountAmount = order.DiscountAmount;
            TaxAmount = order.TaxAmount;
            FinalAmount = order.FinalAmount;
            Email = order.Customer.Email;
            SavedFile = savedFile;
        }
    }

    public interface IPlaceOrderHandler
    {
        OrderResult Handle(PlaceOrderRequest request);
    }

    public interface IProductRepository
    {
        Product GetById(int id);
    }

    public interface IOrderRepository
    {
        int NextId();
        string Save(Order order);
    }

    public interface IAuditLogger
    {
        void Log(string message);
    }

    public class ConsoleCommandParser
    {
        public PlaceOrderRequest? ParseOrder(string name, string customerType, string laptopText, string mouseText)
        {
            if (!int.TryParse(laptopText, out int laptops) || !int.TryParse(mouseText, out int mice))
                return null;

            return new PlaceOrderRequest(name, customerType, laptops, mice);
        }
    }

    public class PolicyValidator
    {
        public void Validate(PlaceOrderRequest request)
        {
            if (request == null)
                throw new ArgumentException("Запит порожній.");
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Ім'я клієнта не може бути порожнім.");
            if (request.CustomerType != "New" && request.CustomerType != "Regular" && request.CustomerType != "VIP")
                throw new ArgumentException("Тип клієнта має бути New, Regular або VIP.");
            if (request.LaptopQuantity <= 0 || request.MouseQuantity <= 0)
                throw new ArgumentException("Кількість має бути більшою за нуль.");
        }
    }

    public class CatalogProductRepository : IProductRepository
    {
        private readonly List<Product> products = new List<Product>
        {
            Product.Create(1, "Ноутбук", 30000m, "Electronics"),
            Product.Create(2, "Мишка", 800m, "Accessories")
        };

        public Product GetById(int id)
        {
            foreach (Product product in products)
            {
                if (product.Id == id)
                    return product;
            }

            throw new ArgumentException("Товар не знайдено.");
        }
    }

    public class FileOrderRepository : IOrderRepository
    {
        private readonly string path;

        public FileOrderRepository(string path)
        {
            this.path = path;
        }

        public int NextId()
        {
            if (!File.Exists(path))
                return 101;

            return 101 + File.ReadAllLines(path).Length;
        }

        public string Save(Order order)
        {
            string line = order.Id + ";" + order.Customer.Name + ";" + order.FinalAmount;
            File.AppendAllText(path, line + Environment.NewLine);
            return path;
        }
    }

    public class FileAuditLogger : IAuditLogger
    {
        private readonly string path;

        public FileAuditLogger(string path)
        {
            this.path = path;
        }

        public void Log(string message)
        {
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - " + message;
            File.AppendAllText(path, line + Environment.NewLine);
        }
    }

    public class OrderController : IPlaceOrderHandler
    {
        private readonly PolicyValidator validator;
        private readonly IProductRepository products;
        private readonly IOrderRepository orders;
        private readonly IAuditLogger logger;

        public OrderController(PolicyValidator validator, IProductRepository products, IOrderRepository orders, IAuditLogger logger)
        {
            this.validator = validator;
            this.products = products;
            this.orders = orders;
            this.logger = logger;
        }

        public OrderResult Handle(PlaceOrderRequest request)
        {
            validator.Validate(request);

            Customer customer = Customer.Create(1, request.Name, "client@example.com", request.CustomerType, 15000m);
            Product laptop = products.GetById(1);
            Product mouse = products.GetById(2);
            Order order = Order.Create(
                orders.NextId(),
                customer,
                (laptop, request.LaptopQuantity),
                (mouse, request.MouseQuantity));

            string savedFile = orders.Save(order);
            logger.Log("Успішно оброблено замовлення " + order.Id);
            return new OrderResult(order, savedFile);
        }
    }

    public class OrderConsole
    {
        private readonly ConsoleCommandParser parser;
        private readonly IPlaceOrderHandler handler;

        public OrderConsole(ConsoleCommandParser parser, IPlaceOrderHandler handler)
        {
            this.parser = parser;
            this.handler = handler;
        }

        public void Run()
        {
            bool running = true;
            while (running)
            {
                Console.WriteLine("=== Оформлення замовлення ===");
                Console.WriteLine("1 - нове замовлення");
                Console.WriteLine("0 - вихід");
                Console.Write("Команда: ");
                string command = Console.ReadLine() ?? "";

                if (command == "0")
                {
                    running = false;
                    continue;
                }

                if (command != "1")
                {
                    Console.WriteLine("Невідома команда.");
                    continue;
                }

                Console.Write("Введіть ім'я клієнта: ");
                string name = Console.ReadLine() ?? "";
                Console.Write("Введіть тип клієнта (New/Regular/VIP): ");
                string customerType = Console.ReadLine() ?? "";
                Console.Write("Кількість ноутбуків: ");
                string laptops = Console.ReadLine() ?? "";
                Console.Write("Кількість мишок: ");
                string mice = Console.ReadLine() ?? "";

                PlaceOrderRequest? request = parser.ParseOrder(name, customerType, laptops, mice);
                if (request == null)
                {
                    Console.WriteLine("Кількість має бути цілим числом.");
                    continue;
                }

                try
                {
                    OrderResult result = handler.Handle(request);
                    Console.WriteLine("Замовлення #" + result.Id + " для " + result.CustomerName);
                    Console.WriteLine("Сума: " + result.TotalAmount + " грн");
                    Console.WriteLine("Знижка: " + result.DiscountAmount + " грн");
                    Console.WriteLine("ПДВ: " + result.TaxAmount + " грн");
                    Console.WriteLine("До сплати: " + result.FinalAmount + " грн");
                    Console.WriteLine("[БД] Замовлення #" + result.Id + " збережено у файл " + result.SavedFile + ".");
                    Console.WriteLine("[Email] Повідомлення відправлено на " + result.Email);
                }
                catch (ArgumentException error)
                {
                    Console.WriteLine(error.Message);
                }
            }
        }
    }

    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            IPlaceOrderHandler handler = new OrderController(
                new PolicyValidator(),
                new CatalogProductRepository(),
                new FileOrderRepository("orders.txt"),
                new FileAuditLogger("audit.log"));

            new OrderConsole(new ConsoleCommandParser(), handler).Run();
        }
    }
}
