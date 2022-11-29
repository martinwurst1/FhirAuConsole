using System.Xml;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Newtonsoft.Json;

namespace FhirAuConsole.FHIR
{
    public class ResourceProxy
    {
        public string Url { get; }
        public string Version { get; }
        public Resource Resource { get; }

        public ResourceProxy(Resource resource)
        {
            this.Resource = resource;
            this.Url = resource.NamedChildren.Where(c => c.ElementName.Equals("url")).Select(c => c.Value.ToString()).FirstOrDefault();
            this.Version = resource.NamedChildren.Where(c => c.ElementName.Equals("version")).Select(c => c.Value.ToString()).FirstOrDefault() ?? string.Empty;
        }

        public static ResourceProxy Create(string archiveMember, Stream stream)
        {
            var extension = Path.GetExtension(archiveMember);
            var isJson = extension.Equals(".json", StringComparison.OrdinalIgnoreCase);
            var isXml = extension.Equals(".xml", StringComparison.OrdinalIgnoreCase);

            if (!isJson && !isXml)
                return null;

            try
            {
                Resource parsedResource;
                if (isJson)
                {
                    using (var streamReader = new StreamReader(stream))
                    using (var jsonReader = new JsonTextReader(streamReader))
                        parsedResource = new FhirJsonParser(new ParserSettings { AcceptUnknownMembers = true, AllowUnrecognizedEnums = true }).Parse<Resource>(jsonReader);
                }
                else
                {
                    using (var xmlReader = new XmlTextReader(stream))
                        parsedResource = new FhirXmlParser(new ParserSettings { AcceptUnknownMembers = true, AllowUnrecognizedEnums = true }).Parse<Resource>(xmlReader);
                }
                if (string.IsNullOrWhiteSpace(parsedResource.NamedChildren.Where(c => c.ElementName.Equals("url")).Select(c => c.Value.ToString()).FirstOrDefault()))
                    return null;
                else
                    return new ResourceProxy(parsedResource);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
