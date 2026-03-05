using System.Reflection;

var asmPath = Path.Combine(AppContext.BaseDirectory, "MudBlazor.dll");
var asm = Assembly.LoadFrom(asmPath);

Type[] types;
try { types = asm.GetTypes(); }
catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray()!; }

// Check MudAvatarGroup
var avatarGroup = types.FirstOrDefault(t => t.Name == "MudAvatarGroup");
if (avatarGroup != null)
{
    Console.WriteLine("--- MudAvatarGroup ---");
    var props = avatarGroup.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
    foreach (var p in props)
    {
        var paramAttr = p.GetCustomAttributes().Any(a => a.GetType().Name == "ParameterAttribute") ? "[Parameter]" : "";
        Console.WriteLine($"  {paramAttr} {p.Name} : {p.PropertyType}");
    }
}

// Check MudTabs
var tabs = types.FirstOrDefault(t => t.Name == "MudTabs");
if (tabs != null)
{
    Console.WriteLine("\n--- MudTabs ---");
    var props = tabs.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
    foreach (var p in props)
    {
        var paramAttr = p.GetCustomAttributes().Any(a => a.GetType().Name == "ParameterAttribute") ? "[Parameter]" : "";
        Console.WriteLine($"  {paramAttr} {p.Name} : {p.PropertyType}");
    }
}

// Check MudTabPanel
var tabPanel = types.FirstOrDefault(t => t.Name == "MudTabPanel");
if (tabPanel != null)
{
    Console.WriteLine("\n--- MudTabPanel ---");
    var props = tabPanel.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
    foreach (var p in props)
    {
        var paramAttr = p.GetCustomAttributes().Any(a => a.GetType().Name == "ParameterAttribute") ? "[Parameter]" : "";
        Console.WriteLine($"  {paramAttr} {p.Name} : {p.PropertyType}");
    }
}

// Check MudChip
var chip = types.FirstOrDefault(t => t.Name == "MudChip`1");
if (chip != null)
{
    Console.WriteLine("\n--- MudChip<T> ---");
    var props = chip.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
    foreach (var p in props)
    {
        var paramAttr = p.GetCustomAttributes().Any(a => a.GetType().Name == "ParameterAttribute") ? "[Parameter]" : "";
        Console.WriteLine($"  {paramAttr} {p.Name} : {p.PropertyType}");
    }
}
