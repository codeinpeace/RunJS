using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RunJS.Core
{
    /// <summary>
    /// Attribute used to anote JS Addins
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class JsAddInAttribute : Attribute
    {
        private string name;


        /// <summary>
        /// Initializes a new instance of the <see cref="JsAddInAttribute"/> class.
        /// </summary>
        public JsAddInAttribute() : this(AddInManager.DEFAULT)
        { }
        /// <summary>
        /// Initializes a new instance of the <see cref="JsAddInAttribute"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public JsAddInAttribute(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name
        {
            get { return name; }
        }
    }
}
