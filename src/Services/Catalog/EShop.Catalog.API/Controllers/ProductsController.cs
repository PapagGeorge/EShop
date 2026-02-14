using EShop.Catalog.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace EShop.Catalog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private static readonly List<Product> Products = new()
    {
        new Product
        {
            Id = Guid.Parse("a1b2c3d4-0001-0001-0001-000000000001"),
            Name = "Wireless Mouse",
            Price = 29.99m,
            Category = "Peripherals"
        },
        new Product
        {
            Id = Guid.Parse("a1b2c3d4-0002-0002-0002-000000000002"),
            Name = "USB-C Cable",
            Price = 12.50m,
            Category = "Cables"
        },
        new Product
        {
            Id = Guid.Parse("a1b2c3d4-0003-0003-0003-000000000003"),
            Name = "Mechanical Keyboard",
            Price = 89.99m,
            Category = "Peripherals"
        },
        new Product
        {
            Id = Guid.Parse("a1b2c3d4-0004-0004-0004-000000000004"),
            Name = "HDMI Cable 2m",
            Price = 15.99m,
            Category = "Cables"
        },
        new Product
        {
            Id = Guid.Parse("a1b2c3d4-0005-0005-0005-000000000005"),
            Name = "Webcam HD 1080p",
            Price = 49.99m,
            Category = "Peripherals"
        }
    };

    [HttpGet]
    public ActionResult<List<Product>> GetAll()
    {
        return Ok(Products);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<Product> GetById(Guid id)
    {
        var product = Products.FirstOrDefault(p => p.Id == id);

        if (product is null)
            return NotFound();

        return Ok(product);
    }

    [HttpPost("batch")]
    public ActionResult<List<Product>> GetByIds([FromBody] List<Guid> ids)
    {
        var products = Products.Where(p => ids.Contains(p.Id)).ToList();
        return Ok(products);
    }
}
