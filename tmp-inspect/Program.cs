using System.Reflection;

var asmPath = Path.Combine(AppContext.BaseDirectory, "MudBlazor.dll");
var asm = Assembly.LoadFrom(asmPath);

Type[] types;
try { types = asm.GetTypes(); }
catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray()!; }

// Check ChartOptions and IChartOptions
var chartOptions = types.FirstOrDefault(t => t.Name == "ChartOptions");
if (chartOptions != null)
{
    Console.WriteLine("=== ChartOptions ===");
    Console.WriteLine($"  FullName: {chartOptions.FullName}");
    Console.WriteLine($"  IsInterface: {chartOptions.IsInterface}");
    foreach (var p in chartOptions.GetProperties(BindingFlags.Public | BindingFlags.Instance))
    {
        Console.WriteLine($"  {p.Name} : {p.PropertyType.Name}");
    }
}

var iChartOptions = types.FirstOrDefault(t => t.Name == "IChartOptions");
if (iChartOptions != null)
{
    Console.WriteLine("\n=== IChartOptions ===");
    Console.WriteLine($"  FullName: {iChartOptions.FullName}");
    foreach (var p in iChartOptions.GetProperties(BindingFlags.Public | BindingFlags.Instance))
    {
        Console.WriteLine($"  {p.Name} : {p.PropertyType.Name}");
    }
}

// Find all types implementing IChartOptions
var implementations = types.Where(t => !t.IsInterface && !t.IsAbstract && 
    t.GetInterfaces().Any(i => i.Name == "IChartOptions")).ToArray();
Console.WriteLine("\n=== Types implementing IChartOptions ===");
foreach (var t in implementations)
{
    Console.WriteLine($"\n  --- {t.FullName} ---");
    foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
    {
        Console.WriteLine($"    {p.Name} : {p.PropertyType.Name}");
    }
}
