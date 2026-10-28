using System.Globalization;

namespace Pizzeria;

public interface IDiscountable
{
    decimal ApplyDiscount(decimal percent);
    string GetDiscountInfo();
}

public abstract class MenuItem
{
    private readonly string _name;
    protected decimal _basePrice;

    public string Name => _name;

    protected MenuItem(string name, decimal basePrice)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Название не может быть пустым.", nameof(name));

        if (basePrice < 0)
            throw new ArgumentException("Базовая цена не может быть отрицательной.", nameof(basePrice));

        _name = name;
        _basePrice = basePrice;
    }

    public abstract decimal GetFinalPrice();
    public abstract string GetInfo();
    public virtual string GetCategory() => "Позиция меню";

    protected static void ValidateDiscount(decimal percent)
    {
        if (percent < 0 || percent > 100)
            throw new ArgumentOutOfRangeException(nameof(percent), "Скидка должна быть от 0 до 100 процентов.");
    }
}

public sealed class Pizza : MenuItem, IDiscountable
{
    public int ToppingsCount { get; }

    public Pizza(string name, decimal basePrice, int toppingsCount) : base(name, basePrice)
    {
        if (toppingsCount < 0)
            throw new ArgumentOutOfRangeException(nameof(toppingsCount), "Количество добавок не может быть отрицательным.");

        ToppingsCount = toppingsCount;
    }

    public override decimal GetFinalPrice() => _basePrice + ToppingsCount * 50m;
    public override string GetCategory() => "Пицца";

    public override string GetInfo() =>
        $"{Name} (добавок: {ToppingsCount}) — {GetFinalPrice():0.00} ₽";

    public decimal ApplyDiscount(decimal percent)
    {
        ValidateDiscount(percent);
        decimal price = GetFinalPrice() * (1m - percent / 100m);
        if (ToppingsCount > 3)
            price *= 0.95m;

        return decimal.Round(price, 2, MidpointRounding.AwayFromZero);
    }

    public string GetDiscountInfo() =>
        ToppingsCount > 3
            ? "Дополнительная скидка 5% за более чем 3 добавки"
            : "Дополнительной скидки нет";
}

public sealed class Drink : MenuItem
{
    public int VolumeMl { get; }

    public Drink(string name, decimal basePrice, int volumeMl) : base(name, basePrice)
    {
        if (volumeMl < 0)
            throw new ArgumentOutOfRangeException(nameof(volumeMl), "Объём не может быть отрицательным.");

        VolumeMl = volumeMl;
    }

    public override decimal GetFinalPrice() => _basePrice + VolumeMl * 0.5m;
    public override string GetCategory() => "Напиток";

    public override string GetInfo() =>
        $"{Name} ({VolumeMl} мл) — {GetFinalPrice():0.00} ₽";
}

public sealed class Dessert : MenuItem, IDiscountable
{
    public bool IsTodayFresh { get; }

    public Dessert(string name, decimal basePrice, bool isTodayFresh) : base(name, basePrice)
    {
        IsTodayFresh = isTodayFresh;
    }

    public override decimal GetFinalPrice() =>
        IsTodayFresh ? _basePrice : _basePrice * 0.7m;

    public override string GetCategory() => "Десерт";

    public override string GetInfo() =>
        $"{Name} ({(IsTodayFresh ? "свежий" : "не первой свежести")}) — {GetFinalPrice():0.00} ₽";

    public decimal ApplyDiscount(decimal percent)
    {
        ValidateDiscount(percent);
        decimal price = GetFinalPrice() * (1m - percent / 100m);
        if (!IsTodayFresh)
            price *= 0.90m;

        return decimal.Round(price, 2, MidpointRounding.AwayFromZero);
    }

    public string GetDiscountInfo() =>
        IsTodayFresh
            ? "Дополнительной скидки нет"
            : "Дополнительная скидка 10% на несвежий десерт";
}

public sealed class Combo : MenuItem
{
    private readonly List<MenuItem> _items = new();

    public Combo(string name, IEnumerable<MenuItem> items) : base(name, 0m)
    {
        ArgumentNullException.ThrowIfNull(items);

        foreach (MenuItem item in items)
        {
            if (item is null)
                throw new ArgumentException("Комбо не может содержать null.", nameof(items));

            _items.Add(item);
        }

        if (_items.Count == 0)
            throw new ArgumentException("Комбо должно содержать хотя бы одну позицию.", nameof(items));
    }

    public IReadOnlyList<MenuItem> GetItems() => _items.AsReadOnly();

    public override decimal GetFinalPrice() =>
        decimal.Round(_items.Sum(item => item.GetFinalPrice()) * 0.85m, 2, MidpointRounding.AwayFromZero);

    public override string GetCategory() => "Комбо";

    public override string GetInfo() =>
        $"{Name} ({_items.Count} поз.) со скидкой 15% — {GetFinalPrice():0.00} ₽";
}

public sealed class Order
{
    private readonly List<MenuItem> _items = new();
    private const int MaxItems = 20;

    public void AddItem(MenuItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (_items.Count >= MaxItems)
            throw new InvalidOperationException($"В заказе не может быть больше {MaxItems} позиций.");

        _items.Add(item);
    }

    public IReadOnlyList<MenuItem> GetItems() => _items.AsReadOnly();

    public decimal GetTotal() => _items.Sum(item => item.GetFinalPrice());

    public decimal GetTotalWithDiscount(decimal percent)
    {
        if (percent < 0 || percent > 100)
            throw new ArgumentOutOfRangeException(nameof(percent), "Скидка должна быть от 0 до 100 процентов.");

        return _items.Sum(item =>
            item is IDiscountable discountable
                ? discountable.ApplyDiscount(percent)
                : item.GetFinalPrice());
    }

    public MenuItem? FindMostExpensive() =>
        _items.OrderByDescending(item => item.GetFinalPrice()).FirstOrDefault();

    public void PrintReceipt()
    {
        Console.WriteLine("=== ЧЕК ПИЦЦЕРИИ ===");
        foreach (MenuItem item in _items)
            Console.WriteLine($"[{item.GetCategory()}] {item.GetInfo()}");

        Console.WriteLine(new string('-', 36));
        Console.WriteLine($"ИТОГО: {GetTotal():0.00} ₽");
    }
}

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");

        var order = new Order();

        var pizza1 = new Pizza("Маргарита", 450m, 2);
        var pizza2 = new Pizza("Мясная", 600m, 4);
        var drink1 = new Drink("Кола", 100m, 300);
        var drink2 = new Drink("Вода", 50m, 500);
        var dessert1 = new Dessert("Чизкейк", 220m, true);
        var dessert2 = new Dessert("Круассан", 150m, false);

        order.AddItem(pizza1);
        order.AddItem(pizza2);
        order.AddItem(drink1);
        order.AddItem(drink2);
        order.AddItem(dessert1);
        order.AddItem(dessert2);

        var combo = new Combo("Комбо «Обед»", new MenuItem[] { pizza1, drink1 });
        order.AddItem(combo);

        order.PrintReceipt();

        Console.WriteLine();
        Console.WriteLine($"Итого со скидками 10% на позиции с IDiscountable: {order.GetTotalWithDiscount(10m):0.00} ₽");

        MenuItem? mostExpensive = order.FindMostExpensive();
        Console.WriteLine(mostExpensive is null
            ? "Заказ пуст."
            : $"Самая дорогая позиция: {mostExpensive.Name} — {mostExpensive.GetFinalPrice():0.00} ₽");

        Console.WriteLine();
        Console.WriteLine("Позиции заказа (по возрастанию цены):");
        foreach (MenuItem item in order.GetItems().OrderBy(item => item.GetFinalPrice()))
            Console.WriteLine($"{item.Name}: {item.GetFinalPrice():0.00} ₽");

        // GetItems() возвращает IReadOnlyList<MenuItem>, поэтому метод Add недоступен:
        // order.GetItems().Add(new Drink("Лимонад", 100m, 250));
        // AsReadOnly() также не позволяет изменить исходную коллекцию через представление.
    }
}
