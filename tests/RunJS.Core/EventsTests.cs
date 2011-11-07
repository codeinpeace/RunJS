
namespace RunJS.Core
{
    class EventsTests : JsEventObject
    {
        public EventsTests(ScriptRunner runner)
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

        internal void Test2()
        {
            Fire("test2");
        }
    }
}
