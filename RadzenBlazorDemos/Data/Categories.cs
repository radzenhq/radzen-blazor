using RadzenBlazorDemos.Models.Northwind;
using System;

namespace RadzenBlazorDemos.Data
{
    public class CategoriesData
    {
        public static Category[] Data = new Category[] {
            new Category() {
                CategoryID = 1,
                CategoryName = "Beverages",
                Description = "Soft drinks, coffees, teas, beers, and ales",
                Picture = ""
            },
            new Category() {
                CategoryID = 2,
                CategoryName = "Condiments",
                Description = "Sweet and savory sauces, relishes, spreads, and seasonings",
                Picture = ""
            },
            new Category() {
                CategoryID = 3,
                CategoryName = "Confections",
                Description = "Desserts, candies, and sweet breads",
                Picture = ""
            },
            new Category() {
                CategoryID = 4,
                CategoryName = "Dairy Products",
                Description = "Cheeses",
                Picture = ""
            },
            new Category() {
                CategoryID = 5,
                CategoryName = "Grains/Cereals",
                Description = "Breads, crackers, pasta, and cereal",
                Picture = ""
            },
            new Category() {
                CategoryID = 6,
                CategoryName = "Meat/Poultry",
                Description = "Prepared meats",
                Picture = ""
            },
            new Category() {
                CategoryID = 7,
                CategoryName = "Produce",
                Description = "Dried fruit and bean curd",
                Picture = ""
            },
            new Category() {
                CategoryID = 8,
                CategoryName = "Seafood",
                Description = "Seaweed and fish",
                Picture = ""
            }
        };
    }
}