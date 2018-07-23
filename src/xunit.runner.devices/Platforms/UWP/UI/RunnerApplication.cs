using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Xunit.Runners.ResultChannels;

namespace Xunit.Runners.UI
{
    public abstract class RunnerApplication : Application
    {
        readonly List<Assembly> testAssemblies = new List<Assembly>();

        public bool TerminateAfterExecution { get; set; }
        [Obsolete("Use ResultChannel")]
        public TextWriter Writer { get; set; }
        public IResultChannel ResultChannel { get; set; }
        public bool AutoStart { get; set; }
        public bool Initialized { get; private set; }

        protected abstract void OnInitializeRunner();

        //EDIT BEGIN
        public string Filter { get; set; } = "";


        //will be called if finished
        internal static Action<Dictionary<Abstractions.ITestCase, TestCaseViewModel>> _OnTestsFinished;
        protected virtual void OnTestsFinished(Dictionary<Abstractions.ITestCase, TestCaseViewModel> testCases) { }

        internal static Func<Task> _WaitUntilCanTerminate;
        protected virtual Task WaitUntilCanTerminate() { return Task.FromResult(0); }

        protected RunnerApplication()
        {
            _OnTestsFinished = OnTestsFinished;
            _WaitUntilCanTerminate = async () => { await WaitUntilCanTerminate(); };
        }
        //EDIT END

        protected void AddTestAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            if (!Initialized)
            {
                testAssemblies.Add(assembly);
            }
        }
        
        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Arguments) && e.Arguments.Contains("role") && e.Arguments.Contains("parentprocessid"))
            {
                // Start the VS Test Runner if there's args we recognize
                Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.CreateDefaultUI();
                // Ensure the current window is active
                Window.Current.Activate();

                Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.Run(e.Arguments);
            }
            else
            {
                Resources.MergedDictionaries.Add(new DeviceResources());

                var rootFrame = Window.Current.Content as Frame;

                // Do not repeat app initialization when the Window already has content,
                // just ensure that the window is active
                if (rootFrame == null)
                {
                    rootFrame = new Frame();
                    rootFrame.Navigated += OnNavigated;

                    // Place the frame in the current Window

                    OnInitializeRunner();

                    Initialized = true;

                    RunnerOptions.Current.TerminateAfterExecution = TerminateAfterExecution;
                    RunnerOptions.Current.AutoStart = AutoStart;
                //EDIT BEGIN
                RunnerOptions.Current.Filter = Filter;
                //EDIT END

                    var nav = new Navigator(rootFrame);

#pragma warning disable CS0618 // Type or member is obsolete
                    var runner = new DeviceRunner(testAssemblies, nav, ResultChannel ?? new TextWriterResultChannel(Writer));
#pragma warning restore CS0618 // Type or member is obsolete
                    var hvm = new HomeViewModel(nav, runner);

                    nav.NavigateTo(NavigationPage.Home, hvm);

                    Window.Current.Content = rootFrame;
                }

                // Ensure the current window is active
                Window.Current.Activate();

            //EDIT BEGIN
#if !WINDOWS_APP
                // Hook up the default Back handler
                SystemNavigationManager.GetForCurrentView().BackRequested += (s, args) =>
                {
                    if (rootFrame.CanGoBack)
                    {
                        args.Handled = true;
                        rootFrame.GoBack();
                    }
                };
#endif
            }
            //EDIT END
        }

        void OnNavigated(object sender, NavigationEventArgs e)
        {
            //EDIT BEGIN
#if !WINDOWS_APP
            // Each time a navigation event occurs, update the Back button's visibility
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                ((Frame)sender).CanGoBack ?
                AppViewBackButtonVisibility.Visible :
                AppViewBackButtonVisibility.Collapsed;
#endif
            //EDIT END

            //EDIT BEGIN
            Page page = e.Content as Page;
            if (page != null)
                //EDIT END
                page.DataContext = e.Parameter;
        }
    }
}
