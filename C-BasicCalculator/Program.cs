using BasicCalculator;

Console.WriteLine("C# GrokBot Calculator");
Console.WriteLine("Operations: +  -  *  /");
Console.WriteLine();

if (!TryReadNumber("First number: ", out var a))
{
    return 1;
}

if (!TryReadNumber("Second number: ", out var b))
{
    return 1;
}

Console.Write("Operation (+, -, *, /): ");
var op = Console.ReadLine()?.Trim();

try
{
    var result = op switch
    {
        "+" => Calculator.Add(a, b),
        "-" => Calculator.Subtract(a, b),
        "*" => Calculator.Multiply(a, b),
        "/" => Calculator.Divide(a, b),
        _ => throw new InvalidOperationException("Invalid operation. Use +, -, *, or /."),
    };

    Console.WriteLine($"The result is: {result}");
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    return 1;
}

static bool TryReadNumber(string prompt, out double value)
{
    Console.Write(prompt);
    var input = Console.ReadLine();
    if (double.TryParse(input, out value))
    {
        return true;
    }

    Console.WriteLine("Invalid number.");
    return false;
}
