using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using Xunit.Runners;
using Xunit.Runners.UI;
#if __UNIFIED__
using Foundation;
using UIKit;
#else
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace Xunit.Runner
{
    public class RunnerAppDelegate : FormsApplicationDelegate
    {
        readonly List<Assembly> testAssemblies = new List<Assembly>();

        Assembly executionAssembly;
        FormsRunner runner;
        // class-level declarations

        protected bool AutoStart { get; set; }

        protected bool Initialized { get; set; }

        protected bool TerminateAfterExecution { get; set; }
        protected TextWriter Writer { get; set; }

        //EDIT BEGIN
        public string Filter { get; set; } = "";


        //will be called if finished
        internal static Action<Dictionary<Abstractions.ITestCase, TestCaseViewModel>> _OnTestsFinished;
        protected virtual void OnTestsFinished(Dictionary<Abstractions.ITestCase, TestCaseViewModel> testCases) { }

        internal static Func<Task> _WaitUntilCanTerminate;
        protected virtual Task WaitUntilCanTerminate() { return Task.FromResult(0); }
        //EDIT END

        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication uiApplication, NSDictionary launchOptions)
        {
            Forms.Init();

            //EDIT BEGIN
            _OnTestsFinished = OnTestsFinished;
            _WaitUntilCanTerminate = async () => { await WaitUntilCanTerminate(); };
            //EDIT END

            RunnerOptions.Current.TerminateAfterExecution = TerminateAfterExecution;
            RunnerOptions.Current.AutoStart = AutoStart;

            runner = new FormsRunner(executionAssembly, testAssemblies, Writer);

            Initialized = true;

            LoadApplication(runner);

            return base.FinishedLaunching(uiApplication, launchOptions);
        }

        protected void AddExecutionAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            if (!Initialized)
            {
                executionAssembly = assembly;
            }
        }

        protected void AddTestAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            if (!Initialized)
            {
                testAssemblies.Add(assembly);
            }
        }
    }
}