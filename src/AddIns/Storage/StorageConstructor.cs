using System;
using System.IO;
using Jurassic.Library;
using RunJS.Core;

namespace RunJS.AddIn.Storage
{
    /// <summary>
    /// A storage-constructor, used to create Storage-instances in js.
    /// </summary>
    [JsAddIn]
    public class StorageConstructor : ClrFunction
    {
        private const string DEFAULT = "default";
        private ScriptRunner runner;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageConstructor"/> class.
        /// </summary>
        /// <param name="runner">The runner.</param>
        public StorageConstructor(ScriptRunner runner)
            : base(runner.Engine.Function.Prototype, "Storage", runner.Engine.Object.InstancePrototype)
        {
            this.runner = runner;

            PopulateFunctions();
        }

        //     JAVASCRIPT INTERNAL FUNCTIONS
        //_________________________________________________________________________________________

        /// <summary>
        /// Constructs a javascript Storage instance.
        /// </summary>
        /// <returns>A new javascript Storage instance.</returns>
        [JSConstructorFunction]
        public StorageInstance Construct()
        {
            return Construct(DEFAULT, false);
        }

        /// <summary>
        /// Constructs a javascript Storage instance.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>A new javascript Storage instance.</returns>
        [JSConstructorFunction]
        public StorageInstance Construct(string name)
        {
            return Construct(name, false);
        }

        /// <summary>
        /// Constructs a javascript Storage instance.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="absolute">if set to <c>true</c> path is treated as an abosolute path.</param>
        /// <returns></returns>
        [JSConstructorFunction]
        public StorageInstance Construct(string name, bool absolute)
        {
            var path = absolute ? Path.Combine(Environment.CurrentDirectory, name) : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RunJS", "Data", GetLegalDataDirName(name));
            if (name == InMemory)
                path = name;
            return new StorageInstance(InstancePrototype, runner, path);
        }


        /// <summary>
        /// A constant value signaling that the database should be created in memory.
        /// </summary>
        [JSProperty(Name = "inMemory")]
        public static string InMemory
        {
            get
            {
                return "$InMemoryDatabase$";
            }
        }

        private static string GetLegalDataDirName(string name)
        {
            foreach (var illegalChar in Path.GetInvalidFileNameChars())
                name = name.Replace(illegalChar.ToString(), "");
            name = name.Replace(' ', '_');
            if (name.Length == 0)
                name = DEFAULT;

            return name;
        }
    }
}
