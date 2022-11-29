// See https://aka.ms/new-console-template for more information

using System.Reflection;
using System.Xml.Linq;
using FhirAuConsole.FHIR;
using Hl7.Fhir.Specification.Source;

Console.WriteLine("Starting validation.");
var resourceResolver = new LocalResourceResolver();
var fhirbundle = Assembly.GetExecutingAssembly().GetManifestResourceStream("FhirAuConsole.FhirBundle.xml");
var xdocument = XDocument.Load(fhirbundle);
var result = FhirValidator.ValidateFhirBundle(xdocument.ToString(), resourceResolver as IResourceResolver, resourceResolver as IAsyncResourceResolver);

Console.WriteLine("Validation complete: " + Environment.NewLine + String.Join("\n", result));
Console.ReadKey();
