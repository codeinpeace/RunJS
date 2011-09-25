using System;
using System.IO;
using Jurassic;
using Jurassic.Library;
using Newtonsoft.Json;
using Raven.Client;
using Raven.Json.Linq;
using RunJS.Core;

namespace RunJS.AddIn.Storage
{
    /// <summary>
    /// A storage js instance.
    /// </summary>
    public class StorageInstance : ObjectInstance
    {
        ScriptRunner runner;
        string path;
        IDocumentStore store;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageInstance"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        /// <param name="runner">The runner.</param>
        /// <param name="path">The path.</param>
        public StorageInstance(ObjectInstance prototype, ScriptRunner runner, string path)
            : base(prototype)
        {
            this.runner = runner;
            this.path = path;
            this.store = new Raven.Client.Embedded.EmbeddableDocumentStore
            {
                DataDirectory = path
            }.Initialize();

            PopulateFunctions();
        }

        /// <summary>
        /// Gets the storage.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>A named proxy storage container</returns>
        [JSFunction(Name = "getStorage")]
        public StorageContainerInstance GetStorage(string name)
        {
            return new StorageContainerInstance(this, name);
        }

        /// <summary>
        /// Puts the specified object at name/id.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="id">The id.</param>
        /// <param name="data">The data.</param>
        /// <returns>The id.</returns>
        [JSFunction(Name = "put")]
        public string Put(string name, string id, object data)
        {
            string json = JSONObject.Stringify(Engine, data);
            Guid? etag = null;
            using (var session = store.OpenSession())
            {
                RavenJObject document = RavenJObject.Parse(json);
                if (document.ContainsKey("$id"))
                    document.Remove("$id");
                if (document.ContainsKey("$etag"))
                {
                    try { etag = Guid.Parse(document["$etag"].Value<string>()); }
                    catch { etag = null; }
                    document.Remove("$etag");
                }
                if (document.ContainsKey("$store"))
                    document.Remove("$store");

                if (String.IsNullOrWhiteSpace(id))
                {
                    id = GetNewId(session, name);
                }

                string dId = name + "/" + id;
                var metadata = new RavenJObject();
                metadata.Add("Raven-Entity-Name", name);

                session.Advanced.DatabaseCommands.Put(dId, etag, document, metadata);

                return id;
            }
        }

        /// <summary>
        /// Gets the specified name/id.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="id">The id.</param>
        /// <returns>The object, or Null.Value if none is found.</returns>
        [JSFunction(Name = "get")]
        public object Get(string name, string id)
        {
            using (var session = store.OpenSession())
            {
                var document = session.Advanced.DatabaseCommands.Get(name + "/" + id);
                if (document == null)
                    return Null.Value;

                var jsonDoc = (RavenJObject)document.DataAsJson.CloneToken();
                jsonDoc.Add("$id", document.Key);
                if (document.Etag != null)
                    jsonDoc.Add("$etag", document.Etag.Value.ToString());
                jsonDoc.Add("$store", name);
                var json = jsonDoc.ToString();
                return JSONObject.Parse(Engine, json);
            }
        }

        /// <summary>
        /// Drops this instance and deletes the database.
        /// </summary>
        [JSFunction(Name = "drop")]
        public void Drop()
        {
            store.Dispose();
            Directory.Delete(path, true);
        }

        private string GetNewId(IDocumentSession session, string name)
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

        private class IdStore
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("next-id")]
            public int NextId { get; set; }
        }

        /// <summary>
        /// Gets the name of the internal class.
        /// </summary>
        /// <value>
        /// The name of the internal class.
        /// </value>
        protected override string InternalClassName
        {
            get
            {
                return "Storage";
            }
        }

        /// <summary>
        /// A storage container instance (named proxy)
        /// </summary>
        public class StorageContainerInstance : ObjectInstance
        {
            private StorageInstance storage;
            private string name;

            internal StorageContainerInstance(StorageInstance instance, string name)
                : base(instance.Engine.Object.InstancePrototype)
            {
                this.name = name;
                this.storage = instance;
            }

            /// <summary>
            /// Gets the name.
            /// </summary>
            [JSProperty(Name = "name")]
            public string Name
            {
                get { return name; }
            }

            /// <summary>
            /// Puts the specified object at name/id.
            /// </summary>
            /// <param name="id">The id.</param>
            /// <param name="data">The data.</param>
            /// <returns>The id.</returns>
            [JSFunction(Name = "put")]
            public string Put(string id, object data)
            {
                return storage.Put(name, id, data);
            }

            /// <summary>
            /// Gets the specified name/id.
            /// </summary>
            /// <param name="id">The id.</param>
            /// <returns>The object, or Null.Value if none is found.</returns>
            [JSFunction(Name = "get")]
            public object Get(string id)
            {
                return storage.Get(name, id);
            }

            /// <summary>
            /// Gets the name of the internal class.
            /// </summary>
            /// <value>
            /// The name of the internal class.
            /// </value>
            protected override string InternalClassName
            {
                get
                {
                    return "StorageContainer";
                }
            }
        }
    }
}
