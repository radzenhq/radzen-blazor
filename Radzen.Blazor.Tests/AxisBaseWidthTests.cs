using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class AxisBaseWidthTests
    {
        // Minimal concrete AxisBase implementation for testing
        private class TestAxis : AxisBase
        {
            internal override double Size => 0;
        }

        private static MethodInfo GetShouldRefreshChartMethod()
        {
            var method = typeof(AxisBase).GetMethod(
                "ShouldRefreshChart",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null) throw new InvalidOperationException("Could not find ShouldRefreshChart method.");
            return method;
        }

        [Fact]
        public void ShouldRefreshChart_IsTrue_When_WidthParameterChanges_FromNullToValue()
        {
            var axis = new TestAxis();
            axis.Width = null;

            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                ["Width"] = 42
            });

            var method = GetShouldRefreshChartMethod();
            var result = (bool)method.Invoke(axis, new object[] { parameters })!;

            Assert.True(result);
        }

        [Fact]
        public void ShouldRefreshChart_IsFalse_When_WidthParameterNotProvided()
        {
            var axis = new TestAxis();
            axis.Width = 10;

            var parameters = ParameterView.Empty;

            var method = GetShouldRefreshChartMethod();
            var result = (bool)method.Invoke(axis, new object[] { parameters })!;

            Assert.False(result);
        }

        [Fact]
        public void ShouldRefreshChart_IsFalse_When_WidthParameterProvidedWithSameValue()
        {
            var axis = new TestAxis();
            axis.Width = 25;

            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                ["Width"] = 25
            });

            var method = GetShouldRefreshChartMethod();
            var result = (bool)method.Invoke(axis, new object[] { parameters })!;

            Assert.False(result);
        }

        [Fact]
        public void ShouldRefreshChart_IsTrue_When_WidthParameterProvidedWithDifferentValue()
        {
            var axis = new TestAxis();
            axis.Width = 25;

            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                ["Width"] = 100
            });

            var method = GetShouldRefreshChartMethod();
            var result = (bool)method.Invoke(axis, new object[] { parameters })!;

            Assert.True(result);
        }
    }
}