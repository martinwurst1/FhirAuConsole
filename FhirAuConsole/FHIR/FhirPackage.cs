using System.Text.Json.Nodes;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace FhirAuConsole.FHIR
{
    public class FhirPackage
    {
        public string Name { get; private set; } = string.Empty;
        public string Version { get; private set; } = String.Empty;
        public string Description { get; private set; } = String.Empty;
        public List<string> FhirVersions { get; } = new List<string>();
        public List<string> Dependencies { get; } = new List<string>();
        public List<ResourceProxy> ResourceProxys { get; } = new List<ResourceProxy>();

        public static FhirPackage Load(Stream stream)
        {
            var fhirPackage = new FhirPackage();

            using (var archiveReader = ReaderFactory.Open(stream))
            {
                if (archiveReader.ArchiveType != ArchiveType.Tar)
                    throw new NotSupportedException("FHIR Package must be a GZipped Tar file");

                while (archiveReader.MoveToNextEntry())
                {
                    if (archiveReader.Entry.IsDirectory
                        || !Path.GetDirectoryName(archiveReader.Entry.Key).Equals("package", StringComparison.OrdinalIgnoreCase)
                        || Path.GetFileName(archiveReader.Entry.Key).Equals(".index.json", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (Path.GetFileName(archiveReader.Entry.Key).Equals("package.json", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var entryStream = archiveReader.OpenEntryStream())
                        using (var streamReader = new StreamReader(entryStream))
                        {
                            var packageDescription = JsonNode.Parse(streamReader.ReadToEnd()).AsObject();
                            fhirPackage.Name = packageDescription["name"]?.ToString() ?? String.Empty;
                            fhirPackage.Version = packageDescription["version"]?.ToString() ?? String.Empty;
                            fhirPackage.Description = packageDescription["description"]?.ToString() ?? String.Empty;

                            var fhirVersions = packageDescription["fhirVersions"]?.AsArray()??
                                packageDescription["fhir-version-list"]?.AsArray();
                            if (fhirVersions != null)
                                foreach (var fhirVersion in fhirVersions)
                                    fhirPackage.FhirVersions.Add(fhirVersion.ToString());

                            var dependencies = packageDescription["dependencies"]?.AsObject();
                            if (dependencies != null)
                                foreach (var dependency in dependencies)
                                    fhirPackage.Dependencies.Add(dependency.ToString().Replace("\"", string.Empty).Replace(":", "|").Replace(" ", string.Empty));
                        }
                    }
                    else if (Path.GetExtension(archiveReader.Entry.Key).Equals(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var entryStream = archiveReader.OpenEntryStream())
                        {
                            var resourceProxy = ResourceProxy.Create(archiveReader.Entry.Key, entryStream);
                            if (resourceProxy != null)
                                fhirPackage.ResourceProxys.Add(resourceProxy);
                        }
                    }
                }
            }
            return fhirPackage;
        }
    }
}
