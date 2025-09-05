namespace RadzenBlazorDemos.Pages
{
    public partial class SpiderChartBasic
    {
        private class ProductComparison
        {
            public string Product { get; set; }
            public string Feature { get; set; }
            public double Score { get; set; }
        }

        private ProductComparison[] productData = new ProductComparison[]
        {
            // Product A
            new ProductComparison { Product = "Product A", Feature = "Performance", Score = 0 },
            new ProductComparison { Product = "Product A", Feature = "Reliability", Score = 92 },
            new ProductComparison { Product = "Product A", Feature = "Usability", Score = 12 },
            new ProductComparison { Product = "Product A", Feature = "Features", Score = 95 },
            new ProductComparison { Product = "Product A", Feature = "Support", Score = 100 },
            new ProductComparison { Product = "Product A", Feature = "Value", Score = 73 },

            // Product B
            new ProductComparison { Product = "Product B", Feature = "Performance", Score = 78 },
            new ProductComparison { Product = "Product B", Feature = "Reliability", Score = 88 },
            new ProductComparison { Product = "Product B", Feature = "Usability", Score = 93 },
            new ProductComparison { Product = "Product B", Feature = "Features", Score = 82 },
            new ProductComparison { Product = "Product B", Feature = "Support", Score = 75 },
            new ProductComparison { Product = "Product B", Feature = "Value", Score = 90 }
        };
    }
}