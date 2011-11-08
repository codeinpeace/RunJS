
using Jurassic.Library;
namespace RunJS.Core
{
    class EventsTestsObject : JsEventObject
    {
        public EventsTestsObject(ScriptRunner runner)
            : base(runner.Construct<JsEventObject>(), runner)
        {
            PopulateFunctions();
        }

        public void Test()
        {
            Fire("test");
        }

        public void Test(string variable)
        {
            Fire("test", variable);
        }

        public void Test2()
        {
            Fire("test2");
        }
    }

    class EventsTest2Constructor : ClrFunction
    {
        private ScriptRunner runner;

        public EventsTest2Constructor(ScriptRunner runner)
            : base(runner.Engine.Function.InstancePrototype, "EventTest2", runner.Construct<JsEventObject>())
        {
            this.runner = runner;
            PopulateFunctions();
        }

        [JSConstructorFunction]
        public EventsTest2 Construct()
        {
            return new EventsTest2(InstancePrototype, runner);
        }
    }

    class EventsTest2 : JsEventObject
    {
        public EventsTest2(ObjectInstance prototype, ScriptRunner runner)
            : base(prototype, runner)
        {

        }

        public void Test()
        {
            Fire("test");
        }
    }
}
