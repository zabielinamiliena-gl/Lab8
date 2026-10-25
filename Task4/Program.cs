using System;
using System.Collections.Generic;

namespace Variant1_OrderProcessing
{

    public enum CustomerType
    {
        New,
        Regular,
        VIP
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public CustomerType Type { get; set; }
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

        public decimal Subtotal => Product.Price * Quantity;
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

    public interface IDiscountStrategy
    {
        bool CanApply(CustomerType customerType);
        decimal CalculateDiscount(decimal totalAmount);
    }

    public class VipDiscountStrategy : IDiscountStrategy
    {
        public bool CanApply(CustomerType customerType) => customerType == CustomerType.VIP;
        public decimal CalculateDiscount(decimal totalAmount) => totalAmount * 0.15m;
    }

    public class RegularDiscountStrategy : IDiscountStrategy
    {
        public bool CanApply(CustomerType customerType) => customerType == CustomerType.Regular;
        public decimal CalculateDiscount(decimal totalAmount) => totalAmount * 0.05m;
    }

    public class NewCustomerDiscountStrategy : IDiscountStrategy
    {
        public bool CanApply(CustomerType customerType) => customerType == CustomerType.New;
        public decimal CalculateDiscount(decimal totalAmount) => 0m;
    }

    public interface ITaxCalculator
    {
        decimal CalculateTax(decimal taxableAmount);
    }

    public class VatTaxCalculator : ITaxCalculator
    {
        private readonly decimal _vatRate;

        public VatTaxCalculator(decimal vatRate = 0.20m)
        {
            _vatRate = vatRate;
        }

        public decimal CalculateTax(decimal taxableAmount) => taxableAmount * _vatRate;
    }

    public interface IOrderRepository
    {
        void Save(Order order);
    }

    public class FileOrderRepository : IOrderRepository
    {
        private readonly string _filePath;

        public FileOrderRepository(string filePath = "orders.txt")
        {
            _filePath = filePath;
        }

        public void Save(Order order)
        {
            Console.WriteLine($"[БД] Замовлення #{order.Id} збережено у файл {_filePath}.");
        }
    }

    public interface INotificationService
    {
        void SendConfirmation(Order order);
    }

    public class EmailNotificationService : INotificationService
    {
        public void SendConfirmation(Order order)
        {
            Console.WriteLine($"[Email] Повідомлення відправлено на {order.Customer.Email}");
        }
    }

    public interface IReceiptFormatter
    {
        string Format(Order order);
    }

    public class TextReceiptFormatter : IReceiptFormatter
    {
        public string Format(Order order)
        {
            return $"Замовлення #{order.Id} для {order.Customer.Name}\n" +
                   $"Сума: {order.TotalAmount} грн\n" +
                   $"Знижка: {order.DiscountAmount} грн\n" +
                   $"ПДВ: {order.TaxAmount} грн\n" +
                   $"До сплати: {order.FinalAmount} грн";
        }
    }

    public class OrderProcessor
    {
        private readonly IEnumerable<IDiscountStrategy> _discountStrategies;
        private readonly ITaxCalculator _taxCalculator;
        private readonly IOrderRepository _repository;
        private readonly INotificationService _notificationService;
        private readonly IReceiptFormatter _receiptFormatter;

        public OrderProcessor(
            IEnumerable<IDiscountStrategy> discountStrategies,
            ITaxCalculator taxCalculator,
            IOrderRepository repository,
            INotificationService notificationService,
            IReceiptFormatter receiptFormatter)
        {
            _discountStrategies = discountStrategies;
            _taxCalculator = taxCalculator;
            _repository = repository;
            _notificationService = notificationService;
            _receiptFormatter = receiptFormatter;
        }

        public void ProcessOrder(Order order)
        {
            decimal subtotal = 0m;
            foreach (var item in order.Items)
            {
                subtotal += item.Subtotal;
            }
            order.TotalAmount = subtotal;

            decimal discount = 0m;
            foreach (var strategy in _discountStrategies)
            {
                if (strategy.CanApply(order.Customer.Type))
                {
                    discount = strategy.CalculateDiscount(order.TotalAmount);
                    break;
                }
            }
            order.DiscountAmount = discount;

            decimal taxableAmount = order.TotalAmount - order.DiscountAmount;
            order.TaxAmount = _taxCalculator.CalculateTax(taxableAmount);

            order.FinalAmount = taxableAmount + order.TaxAmount;
            order.Status = "Processed";

            string receipt = _receiptFormatter.Format(order);
            Console.WriteLine(receipt);

            _repository.Save(order);
            _notificationService.SendConfirmation(order);
        }
    }


    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("Оформлення замовлення");
            Console.Write("Введіть ім'я клієнта: ");
            string name = Console.ReadLine();

            Console.Write("Введіть тип клієнта (New/Regular/VIP): ");
            string typeInput = Console.ReadLine();

            if (!Enum.TryParse(typeInput, true, out CustomerType customerType))
            {
                customerType = CustomerType.New;
            }

            Customer customer = new Customer
            {
                Id = 1,
                Name = name,
                Email = "client@example.com",
                Type = customerType,
                TotalSpent = 15000m
            };

            Order order = new Order
            {
                Id = 101,
                Customer = customer,
                Status = "New"
            };

            Product p1 = new Product { Id = 1, Name = "Ноутбук", Price = 30000m, Category = "Electronics" };
            Product p2 = new Product { Id = 2, Name = "Мишка", Price = 800m, Category = "Accessories" };

            order.Items.Add(new OrderItem { Product = p1, Quantity = 1 });
            order.Items.Add(new OrderItem { Product = p2, Quantity = 2 });

            var discountStrategies = new List<IDiscountStrategy>
            {
                new VipDiscountStrategy(),
                new RegularDiscountStrategy(),
                new NewCustomerDiscountStrategy()
            };

            OrderProcessor processor = new OrderProcessor(
                discountStrategies,
                new VatTaxCalculator(0.20m),
                new FileOrderRepository("orders.txt"),
                new EmailNotificationService(),
                new TextReceiptFormatter()
            );

            processor.ProcessOrder(order);
        }
    }
}