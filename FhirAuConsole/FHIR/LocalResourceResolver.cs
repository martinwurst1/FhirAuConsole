using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using Hl7.Fhir.ElementModel.Types;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;

namespace FhirAuConsole.FHIR
{
    public class LocalResourceResolver : IResourceResolver, IAsyncResourceResolver
    {
        private readonly Dictionary<string, Dictionary<string, ResourceProxy>> resourcen = new();
        private readonly object lockObject = new();
        private readonly object lockObjectVersion = new();

        internal string CurrentBundleVersion
        {
            get
            {
                lock (this.lockObjectVersion)
                {
                    return this.currentBundleVersion;
                }
            }
            set
            {
                lock (this.lockObjectVersion)
                {
                    this.currentBundleVersion = value;
                }
            }
        }

        private string currentBundleVersion;

        public void InitResources()
        {
            void Add(ResourceProxy resourceProxy)
            {
                void AddOrUpdate(string url, ResourceProxy resource)
                {
                    if (this.resourcen[this.CurrentBundleVersion].ContainsKey(url))
                        this.resourcen[this.CurrentBundleVersion][url] = resource;
                    else
                        this.resourcen[this.CurrentBundleVersion].Add(url, resource);
                }

                AddOrUpdate(resourceProxy.Url, resourceProxy);
                AddOrUpdate(resourceProxy.Url + "|" + resourceProxy.Version, resourceProxy);
            }

            lock (this.lockObject)
            {
                if (this.resourcen.ContainsKey(this.CurrentBundleVersion))
                    return;

                this.resourcen.Add(this.CurrentBundleVersion, new());
                var packageStreams = GetEmbeddedTarPackages(this.CurrentBundleVersion);
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

        private static List<Stream> GetEmbeddedTarPackages(string version)
        {
            var searchpattern = string.Empty;
            switch (version)
            {
                case "1.0.2": searchpattern = "_1._0._2";
                    break;
                case "1.1.0": searchpattern = "_1._1._0";
                    break;
                default:
                    throw new ArgumentException($"Es können keine FHIR-Bundles für Version {version} geladen werden.");
            }
            var names = Assembly.GetExecutingAssembly().GetManifestResourceNames().
                Where(n => (n.Contains(searchpattern) && n.EndsWith(".tgz")) || 
                           n.Contains("FhirPackages.Basis"));
            
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
            return this.resourcen[this.CurrentBundleVersion].TryGetValue(uri, out var resourceProxy) ? resourceProxy.Resource : null;
        }

        public Resource ResolveByUri(string uri)
        {
            this.InitResources();
            return this.resourcen[this.CurrentBundleVersion].TryGetValue(uri, out var resourceProxy) ? resourceProxy.Resource : null;
        }

        public Task<Resource> ResolveByUriAsync(string uri)
        {
            this.InitResources();
            return System.Threading.Tasks.Task.FromResult(this.resourcen[this.CurrentBundleVersion].TryGetValue(uri, out var resourceProxy) ? resourceProxy.Resource : null);
        }

        public Task<Resource> ResolveByCanonicalUriAsync(string uri)
        {
            this.InitResources();
            return System.Threading.Tasks.Task.FromResult(this.resourcen[this.CurrentBundleVersion].TryGetValue(uri, out var resourceProxy) ? resourceProxy.Resource : null);
        }
    }
}
