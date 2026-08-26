namespace SimpleMapper.Benchmarks;

public class Address
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string PostCode { get; set; } = "";
}

public class AddressDto
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string PostCode { get; set; } = "";
}

public class OrderLine
{
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class OrderLineDto
{
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public Address ShippingAddress { get; set; } = new();
    public List<OrderLine> Lines { get; set; } = new();
}

public class OrderDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public AddressDto ShippingAddress { get; set; } = new();
    public List<OrderLineDto> Lines { get; set; } = new();
}