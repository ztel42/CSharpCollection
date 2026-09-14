using BasicCalculator;
using Xunit;

public class CalculatorTests
{
    [Theory]
    [InlineData(2, 3, 5)]
    [InlineData(-1, 1, 0)]
    [InlineData(1.5, 2.5, 4)]
    public void Add_ReturnsSum(double a, double b, double expected)
    {
        Assert.Equal(expected, Calculator.Add(a, b));
    }

    [Theory]
    [InlineData(5, 3, 2)]
    [InlineData(1, 1, 0)]
    [InlineData(2.5, 0.5, 2)]
    public void Subtract_ReturnsDifference(double a, double b, double expected)
    {
        Assert.Equal(expected, Calculator.Subtract(a, b));
    }

    [Theory]
    [InlineData(4, 3, 12)]
    [InlineData(-2, 3, -6)]
    [InlineData(1.5, 2, 3)]
    public void Multiply_ReturnsProduct(double a, double b, double expected)
    {
        Assert.Equal(expected, Calculator.Multiply(a, b));
    }

    [Theory]
    [InlineData(10, 2, 5)]
    [InlineData(9, 3, 3)]
    [InlineData(1, 4, 0.25)]
    public void Divide_ReturnsQuotient(double a, double b, double expected)
    {
        Assert.Equal(expected, Calculator.Divide(a, b));
    }

    [Fact]
    public void Divide_ByZero_Throws()
    {
        Assert.Throws<DivideByZeroException>(() => Calculator.Divide(10, 0));
    }
}
