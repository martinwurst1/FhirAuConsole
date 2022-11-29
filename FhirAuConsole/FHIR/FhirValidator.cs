using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Validation;

namespace FhirAuConsole.FHIR
{
    public class FhirValidator
    {
        private static Resource ParseXml(string input)
        {
            var parser = new FhirXmlParser
            {
                Settings =
                    {
                        PermissiveParsing = true,
                        AcceptUnknownMembers = true,
                        AllowUnrecognizedEnums = true
                    }
            };
            return parser.Parse<Resource>(input);
        }

        public static List<string> ValidateFhirBundle(string input, IResourceResolver resourceResolver, IAsyncResourceResolver terminologyService)
        {
            ValidationSettings GetSettings()
            {
                var validationSettings = ValidationSettings.CreateDefault();
                validationSettings.ResourceResolver = resourceResolver;
                validationSettings.Trace = true;
                validationSettings.ResolveExternalReferences = true;
                validationSettings.EnableXsdValidation = false;
                validationSettings.GenerateSnapshot = true;
                validationSettings.TerminologyService = new LocalTerminologyService(terminologyService);
                return validationSettings;
            }

            Resource geparstesXml = null;
            try
            {
                geparstesXml = ParseXml(input);
            }
            catch (Exception)
            {
                return new List<string> { "Fehler beim Parsen" };
            }
            var settings = GetSettings();
            var validator = new Hl7.Fhir.Validation.Validator(settings);
            var result = validator.Validate(geparstesXml);
            return result.Success
                ? new List<string>()
                : result.Issue.Where(i => i.Severity != OperationOutcome.IssueSeverity.Information 
                                          && i.Severity != OperationOutcome.IssueSeverity.Warning)
                    .Select(i => $"{i.Expression.FirstOrDefault()}: {i.Details.Text}")
                    .ToList();
        }
    }
}
