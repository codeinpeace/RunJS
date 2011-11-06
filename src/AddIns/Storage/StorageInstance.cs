using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using Jurassic;
using Jurassic.Library;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using RunJS.Core;

namespace RunJS.AddIn.Storage
{
    /// <summary>
    /// A storage js instance.
    /// </summary>
    public class StorageInstance : ObjectInstance
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        static volatile AppDomain ad;
        static readonly object adLock = new object();

        static StorageMbro CreateMbro(string path)
        {
            if (ad == null)
            {
                lock (adLock)
                {
                    if (ad == null)
                    {
                        if (!AppDomain.CurrentDomain.IsHomogenous)
                        {
                            ad = AppDomain.CreateDomain("RavenDomain", null, new AppDomainSetup
                            {
                                ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase
                            }, new PermissionSet(PermissionState.Unrestricted), null);
                        }
                        else
                            ad = AppDomain.CurrentDomain;
                    }
                }
            }

            string name = Assembly.GetExecutingAssembly().FullName;
            var mbro = (StorageMbro)ad.CreateInstanceAndUnwrap(name, typeof(StorageMbro).FullName);
            mbro.Initialize(path == StorageConstructor.InMemory ? null : path);
            return mbro;
        }

        ScriptRunner runner;
        StorageMbro mbro;
        string path;

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
            try
            {
                this.mbro = CreateMbro(path);
            }
            catch (Exception e)
            {
                throw new JavaScriptException(Engine, "Error", "Error occured in mbro creation.", e);
            }

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
            JObject document = JObject.Parse(json);
            if (document.Property("$id") != null)
                document.Remove("$id");
            if (document.Property("$etag") != null)
            {
                try { etag = Guid.Parse(document["$etag"].Value<string>()); }
                catch { etag = null; }
                document.Remove("$etag");
            }
            if (document.Property("$store") != null)
                document.Remove("$store");

            if (String.IsNullOrWhiteSpace(id))
            {
                id = mbro.GetNewId(name);
            }

            string dId = name + "/" + id;
            var metadata = new JObject();
            metadata.Add("Raven-Entity-Name", name);
            metadata.Add("Raven-Clr-Type", typeof(ObjectInstance).FullName);

            mbro.Put(dId, etag, document.ToString(), metadata.ToString());

            return id;
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
            var document = mbro.Get(name + "/" + id);
            if (document == null)
                return Null.Value;

            var jsonDoc = JObject.Parse(document);
            jsonDoc.Remove("@metadata");
            jsonDoc.Add("$store", name);
            var json = jsonDoc.ToString();
            return JSONObject.Parse(Engine, json);
        }

        /// <summary>
        /// Queries the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="query">The query.</param>
        /// <returns>A list of items matching the query</returns>
        [JSFunction(Name = "query")]
        public object Query(string name, params object[] query)
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
            var qs = mbro.Query(name, sQuery);
            var res = JArray.Parse(qs);
            var transformed = res.Select(t =>
            {
                var obj = (JObject)t;
                var metadata = (JObject)obj["@metadata"].DeepClone();
                obj.Remove("@metadata");
                obj.Add("$id", metadata["@id"]);
                if (metadata.Property("@etag") != null)
                    obj.Add("$etag", metadata["@etag"]);
                obj.Add("$store", name);
                return obj.ToString();
            }).Select(json => JSONObject.Parse(Engine, json));
            return Engine.Array.New(transformed.ToArray());
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
            mbro.Dispose();
            mbro = null;
            Thread.Sleep(100);
            try
            {
                if (path != null)
                    Directory.Delete(path, true);
            }
            catch
            { }
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
            public object Query(params object[] query)
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
