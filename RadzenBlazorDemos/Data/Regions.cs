using RadzenBlazorDemos.Models.Northwind;
using System;

namespace RadzenBlazorDemos.Data
{
    public class RegionsData
    {
        public static Region[] Data = new Region[] {
            new Region() {
                RegionID = 1,
                RegionDescription = "Eastern"
            },
            new Region() {
                RegionID = 2,
                RegionDescription = "Western"
            },
            new Region() {
                RegionID = 3,
                RegionDescription = "Northern"
            },
            new Region() {
                RegionID = 4,
                RegionDescription = "Southern"
            }
        };
    }
}