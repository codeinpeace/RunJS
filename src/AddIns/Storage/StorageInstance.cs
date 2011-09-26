using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Jurassic;
using Jurassic.Library;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Raven.Abstractions.Data;
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
            if (!Directory.Exists(path))
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (Exception e)
                {
                    throw new JavaScriptException(Engine, "Error", "Error creating directory", e);
                }
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
            if (id == "null")
                id = null;
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
        /// Queries the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="query">The query.</param>
        /// <returns>A list of items matching the query</returns>
        [JSFunction(Name = "query")]
        public ArrayInstance Query(string name, params object[] query)
        {
            var queryDicts = query.Select(q =>
            {
                var json = JSONObject.Stringify(Engine, q);
                var jObj = JObject.Parse(json);
                return FlattenObject(jObj);
            });

            StringBuilder sb = new StringBuilder();
            foreach (var qd in queryDicts)
            {
                sb.Append("(");
                foreach (var q in qd)
                {
                    sb.Append(q.Key).Append(":");
                    string val = q.Value;
                    bool whiteSpaces = val.Contains(' ');
                    if (whiteSpaces)
                        sb.Append("\"");
                    sb.Append(val);
                    if (whiteSpaces)
                        sb.Append("\"");

                    sb.Append(" AND ");
                }
                sb.Remove(sb.Length - 5, 5);
                sb.Append(") OR ");
            }
            sb.Remove(sb.Length - 4, 4);
            var sQuery = sb.ToString().Trim();
            Console.WriteLine("About to query dynamic/{1}: {0}", sQuery, name);
            using (var session = store.OpenSession())
            {
                var iq = new IndexQuery();
                iq.Query = sQuery;
                var qs = session.Advanced.DatabaseCommands.Query("dynamic/" + name, iq, new string[0]);
                Console.WriteLine(qs.Results);
                Console.WriteLine(qs.Results.Count);
                if (qs.Results == null)
                    return Engine.Array.New();
                return Engine.Array.New(
                    qs.Results.Select(r =>
                    {
                        var rJson = r.ToString();
                        return JSONObject.Parse(Engine, rJson);
                    }).ToArray()
                );
                //var qry = session.Advanced.LuceneQuery<JObject>("dynamic/" + name);
                //qry = qry.Where(sQuery);
                //return Engine.Array.New(qry.Select(jObj => JSONObject.Parse(Engine, jObj.ToString())).ToArray());
            }
        }

        private Dictionary<string, string> FlattenObject(JObject obj, string prefix = "")
        {
            var dict = new Dictionary<string, string>();
            foreach (var prop in obj)
            {
                switch (prop.Value.Type)
                {
                    case JTokenType.Object:
                        var nDict = FlattenObject(prop.Value.Value<JObject>(), prop.Key + ".");
                        foreach (var v in nDict)
                            dict.Add(v.Key, v.Value);
                        break;
                    case JTokenType.Boolean:
                    case JTokenType.Date:
                    case JTokenType.Float:
                    case JTokenType.Integer:
                    case JTokenType.String:
                        dict.Add(prefix + prop.Key, prop.Value.Value<string>());
                        break;
                    default:
                        break;
                }
            }
            return dict;
        }

        /// <summary>
        /// Drops this instance and deletes the database.
        /// </summary>
        [JSFunction(Name = "drop")]
        public void Drop()
        {
            store.Dispose();
            Thread.Sleep(100);
            try
            {
                Directory.Delete(path, true);
            }
            catch
            { }
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

                PopulateFunctions();
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
            /// Queries the specified name.
            /// </summary>
            /// <param name="query">The query.</param>
            /// <returns>A list of items matching the query</returns>
            [JSFunction(Name = "query")]
            public ArrayInstance Query(params object[] query)
            {
                return storage.Query(name, query);
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
