// See https://aka.ms/new-console-template for more information

using app;
using demo.generated;

foreach (var it in new App().ListMethodDescriptions())
{
    Console.WriteLine($"{it.Key}={it.Value}");
}
