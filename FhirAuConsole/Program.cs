// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;
using FhirAuConsole.FHIR;
using Hl7.Fhir.Specification.Source;

Console.WriteLine("Do you want to validate: a) 1.0.2 or b) 1.1.0 ?");
var input = Console.ReadLine();
if (input.ToLower() != "a" && input.ToLower() != "b")
{
    Console.WriteLine("false input, exit application");
    return;
}

Console.WriteLine("Starting validation");
var resourceResolver = new LocalResourceResolver();
var fhirbundle = input == "a" ? 
    Assembly.GetExecutingAssembly().GetManifestResourceStream("FhirAuConsole.FhirBundle102.xml") :
    Assembly.GetExecutingAssembly().GetManifestResourceStream("FhirAuConsole.FhirBundle110.xml");
var xdocument = XDocument.Load(fhirbundle);
var result = FhirValidator.ValidateFhirBundle(xdocument.ToString(), resourceResolver as IResourceResolver, resourceResolver as IAsyncResourceResolver);
var mem = Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024);
Console.WriteLine("Validation complete");
//if (result.Any())
//    Console.WriteLine("The following validation errors occured: " + Environment.NewLine + String.Join("\n", result));
Console.WriteLine($"Current memory usage: {mem} mb" );
Console.ReadKey();
