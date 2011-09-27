using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Json.Linq;

namespace RunJS.AddIn.Storage
{
    class StorageMbro : MarshalByRefObject, IDisposable
    {
        string path;
        IDocumentStore store;

        public StorageMbro()
        {
        }

        public void Initialize(string path)
        {
            if (this.path != null)
                throw new InvalidOperationException("Already initialized");

            this.path = path;
            if (path != null)
            {
                if (!Directory.Exists(path))
                    try
                    {
                        Directory.CreateDirectory(path);
                    }
                    catch (Exception e)
                    {
                        throw new IOException("Error in creating directory", e);
                    }
                this.store = new Raven.Client.Embedded.EmbeddableDocumentStore
                {
                    DataDirectory = path
                }.Initialize();
            }
            else
            {
                this.store = new Raven.Client.Embedded.EmbeddableDocumentStore
                {
                    RunInMemory = true
                }.Initialize();
            }
        }

        public void Put(string id, Guid? etag, string document, string metadata)
        {
            using (var session = store.OpenSession())
            {
                session.Advanced.DatabaseCommands.Put(id, etag, RavenJObject.Parse(document), RavenJObject.Parse(metadata));
            }
        }

        public string Get(string id)
        {
            using (var session = store.OpenSession())
            {
                var result = session.Advanced.DatabaseCommands.Get(id);
                if (result == null)
                    return null;

                var rd = (RavenJObject)result.DataAsJson.CloneToken();
                rd.Add("@metadata", result.Metadata.CloneToken());
                rd.Add("$id", result.Key);
                if (result.Etag != null)
                    rd.Add("$etag", result.Etag.ToString());
                return rd.ToString();
            }
        }

        public string Query(string dynIndexName, string query)
        {
            using (var session = store.OpenSession())
            {
                var q = new IndexQuery();
                q.Query = query;
                var result = session.Advanced.DatabaseCommands.Query("dynamic/" + dynIndexName, q, new string[0]);
                return "[" + String.Join(",", result.Results.Select(r => r.ToString())) + "]";
            }
        }

        public string GetNewId(string name)
        {
            using (var session = store.OpenSession())
            {
                var idStore = session.Load<IdStore>("idstore/" + name.Replace(' ', '_'));
                if (idStore == null)
                {
                    idStore = new IdStore
                    {
                        Id = "idstore/" + name.Replace(' ', '_'),
                        NextId = 1
                    };
                    session.Store(idStore);
                }

                var newId = idStore.NextId++;
                session.SaveChanges();
                return newId.ToString();
            }
        }

        #region IDisposable Members
        public void Dispose()
        {
            store.Dispose();
            store = null;
        }
        #endregion
    }

    internal class IdStore
    {
        public string Id { get; set; }

        [JsonProperty("next-id")]
        public int NextId { get; set; }
    }
}
