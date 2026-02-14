namespace EShop.Ordering.Domain.ValueObjects;

public class Address : IEquatable<Address>
{
    public string Street { get; }
    public string City { get; }
    public string ZipCode { get; }
    public string Country { get; }

    public Address(string street, string city, string zipCode, string country)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street cannot be empty.", nameof(street));
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City cannot be empty.", nameof(city));
        if (string.IsNullOrWhiteSpace(zipCode))
            throw new ArgumentException("ZipCode cannot be empty.", nameof(zipCode));
        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country cannot be empty.", nameof(country));

        Street = street;
        City = city;
        ZipCode = zipCode;
        Country = country;
    }

    public bool Equals(Address? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Street == other.Street
            && City == other.City
            && ZipCode == other.ZipCode
            && Country == other.Country;
    }

    public override bool Equals(object? obj) => Equals(obj as Address);

    public override int GetHashCode() => HashCode.Combine(Street, City, ZipCode, Country);

    public static bool operator ==(Address? left, Address? right) => Equals(left, right);
    public static bool operator !=(Address? left, Address? right) => !Equals(left, right);
}
