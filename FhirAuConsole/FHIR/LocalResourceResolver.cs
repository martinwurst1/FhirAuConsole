using System.Reflection;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;

namespace FhirAuConsole.FHIR
{
    public class LocalResourceResolver : IResourceResolver, IAsyncResourceResolver
    {
        private readonly Dictionary<string, ResourceProxy> resourcen = new();
        private readonly object lockObject = new();

        public void InitResources()
        {
            void Add(ResourceProxy resourceProxy)
            {
                void AddOrUpdate(string url, ResourceProxy resource)
                {
                    if (this.resourcen.ContainsKey(url))
                        this.resourcen[url] = resource;
                    else
                        this.resourcen.Add(url, resource);
                }

                AddOrUpdate(resourceProxy.Url, resourceProxy);
                AddOrUpdate(resourceProxy.Url + "|" + resourceProxy.Version, resourceProxy);
            }

            lock (this.lockObject)
            {
                if (this.resourcen.Any())
                    return;

                var packageStreams = GetEmbeddedTarPackages();
                foreach (var stream in packageStreams)
                {
                    var package = FhirPackage.Load(stream);
                    foreach (var resource in package.ResourceProxys)
                        Add(resource);
                }
                //Das Bundle.zip ist ein selbst erzeugtes Zipfile mit den Resourcen, die in den Tar-Packages nicht korrekt/vollständig waren. Bei einem Update der Tar-Zips
                //sollte geprüft werden, ob die Dateien im Bundle.zip noch notwendig sind.
                //Dadurch, dass sie nach den Tar-Zips in das Dictionary gesteckt werden, überschreiben sie anderen Resourcen.
                this.GetCustomResources().ForEach(Add);
            }
        }

        private static List<Stream> GetEmbeddedTarPackages()
        {
            var names = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(n => n.EndsWith(".tgz")).OrderBy(name => name);
            return names.Select(name => Assembly.GetExecutingAssembly().GetManifestResourceStream(name)).ToList();
        }

        private List<ResourceProxy> GetCustomResources()
        {
            var zipStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FhirAuConsole.FhirPackages.Bundle.zip");
            var resources = new List<ResourceProxy>();
            using var archiveReader = SharpCompress.Readers.ReaderFactory.Open(zipStream);
            while (archiveReader.MoveToNextEntry())
            {
                using var entryStream = archiveReader.OpenEntryStream();
                var resourceProxy = ResourceProxy.Create(archiveReader.Entry.Key, entryStream);
                if (resourceProxy != null)
                    resources.Add(resourceProxy);
            }

            return resources;
        }

        public Resource ResolveByCanonicalUri(string uri)
        {
            this.InitResources();
            return this.resourcen.TryGetValue(uri, out var resourceProxy) ? resourceProxy.Resource : null;
        }

        public Resource ResolveByUri(string uri)
        {
            this.InitResources();
            return this.resourcen.TryGetValue(uri, out var resourceProxy) ? resourceProxy.Resource : null;
        }

        public Task<Resource> ResolveByUriAsync(string uri)
        {
            this.InitResources();
            return System.Threading.Tasks.Task.FromResult(this.resourcen.TryGetValue(uri, out var resourceProxy) ? resourceProxy.Resource : null);
        }

        public Task<Resource> ResolveByCanonicalUriAsync(string uri)
        {
            this.InitResources();
            return System.Threading.Tasks.Task.FromResult(this.resourcen.TryGetValue(uri, out var resourceProxy) ? resourceProxy.Resource : null);
        }
    }
}
