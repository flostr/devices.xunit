using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xunit.Runners.UI;


namespace Xunit.Runners
{
    public class HomeViewModel : ViewModelBase
    {
        readonly INavigation navigation;
        readonly ITestRunner runner;
        readonly DelegateCommand runEverythingCommand;

        public event EventHandler ScanComplete;
        readonly ManualResetEventSlim mre = new ManualResetEventSlim(false);
        bool isBusy;

        //EDIT BEGIN
        //runner config
        private readonly int MaxDiagnosticMessagesLength = 5000;
        private readonly bool ParallelizeAssembly = false;
        private readonly bool ShowDiagnosticMessages = true;
        private readonly int LongRunningTestSeconds = 5;
        //EDIT END

        internal HomeViewModel(INavigation navigation, ITestRunner runner)
        {
            this.navigation = navigation;
            this.runner = runner;
            TestAssemblies = new ObservableCollection<TestAssemblyViewModel>();

            OptionsCommand = new DelegateCommand(OptionsExecute);
            CreditsCommand = new DelegateCommand(CreditsExecute);
            runEverythingCommand = new DelegateCommand(RunEverythingExecute, () => !isBusy);
            NavigateToTestAssemblyCommand = new DelegateCommand<object>(async vm => await navigation.NavigateTo(NavigationPage.AssemblyTestList, vm));

            runner.OnDiagnosticMessage += RunnerOnOnDiagnosticMessage;


            StartAssemblyScan();
        }

        void RunnerOnOnDiagnosticMessage(string s)
        {
            //EDIT BEGIN
            DiagnosticMessages = $"{s}{Environment.NewLine}{Environment.NewLine}" + DiagnosticMessages;

            if (DiagnosticMessages.Length > MaxDiagnosticMessagesLength)
                DiagnosticMessages = DiagnosticMessages.Substring(0, MaxDiagnosticMessagesLength);
            //EDIT END
        }


        public ObservableCollection<TestAssemblyViewModel> TestAssemblies { get; }

        private string diagnosticMessages = string.Empty;
        public string DiagnosticMessages
        {
            get { return diagnosticMessages;}
            set { Set(ref diagnosticMessages, value); }
        }


        void OptionsExecute()
        {
            Debug.WriteLine("Options");
        }

        async void CreditsExecute()
        {
            await navigation.NavigateTo(NavigationPage.Credits);
        }

        async void RunEverythingExecute()
        {
            try
            {
                IsBusy = true;
                await Run();
            }
            finally
            {
                IsBusy = false;
            }
        }


        public ICommand OptionsCommand { get; private set; }
        public ICommand CreditsCommand { get; private set; }

        public ICommand RunEverythingCommand => runEverythingCommand;

        public ICommand NavigateToTestAssemblyCommand { get; private set; }

        public bool IsBusy
        {
            get { return isBusy; }
            private set
            {
                if (Set(ref isBusy, value))
                {
                    runEverythingCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public async void StartAssemblyScan()
        {
            IsBusy = true;
            try
            {
                var allTests = await runner.Discover();

                // Back on UI thread
                foreach (var vm in allTests)
                {
                    //EDIT BEGIN
                    vm.RunInfo.Configuration.ParallelizeAssembly = ParallelizeAssembly;
                    vm.RunInfo.Configuration.LongRunningTestSeconds = LongRunningTestSeconds;
                    vm.RunInfo.Configuration.DiagnosticMessages = ShowDiagnosticMessages;
                    //EDIT END

                    TestAssemblies.Add(vm);
                }

                var evt = ScanComplete;
                evt?.Invoke(this, EventArgs.Empty);

                mre.Set();

            }
            finally
            {
                IsBusy = false;
            }

            if (RunnerOptions.Current.AutoStart)
            {
                await Task.Run(() => mre.Wait());
                await Run();

                //EDIT BEGIN
#if WINDOWS_APP || WINDOWS_UWP
                if (RunnerApplication._WaitUntilCanTerminate != null)
                    await RunnerApplication._WaitUntilCanTerminate.Invoke();
#elif __IOS__
                if (Xunit.Runner.RunnerAppDelegate._WaitUntilCanTerminate != null)
                    await Xunit.Runner.RunnerAppDelegate._WaitUntilCanTerminate.Invoke();
#endif
                //EDIT END

                if (RunnerOptions.Current.TerminateAfterExecution)
                    PlatformHelpers.TerminateWithSuccess();
            }
        }

        Task Run()
        {
            if (!string.IsNullOrWhiteSpace(DiagnosticMessages))
            {
                DiagnosticMessages += $"-----------{Environment.NewLine}";
            }

            //EDIT BEGIN
            //return runner.Run(TestAssemblies.Select(t => t.RunInfo).ToList(), "Run Everything");
            var tests = TestAssemblies.Select(t => t.RunInfo).ToList();

            if (String.IsNullOrEmpty(RunnerOptions.Current.Filter))
                return runner.Run(tests, "Run Everything");

            //filtered
            var testsFiltered = new List<AssemblyRunInfo>();
            foreach (var test in tests)
            {
                var items = test.TestCases.Where(t => t.DisplayName.Contains(RunnerOptions.Current.Filter)).ToList();

                if (items.Count > 0)
                {
                    test.TestCases.Clear();

                    foreach (var item in items)
                        test.TestCases.Add(item);

                    testsFiltered.Add(test);
                }
            }

            //navigate to filtered if only one assembly
            if (testsFiltered.Count == 1 && NavigateToTestAssemblyCommand.CanExecute(testsFiltered[0]))
            {
                TestAssemblyViewModel.SearchFilter = RunnerOptions.Current.Filter;
                NavigateToTestAssemblyCommand.Execute(testsFiltered[0]);
            }

            return runner.Run(testsFiltered, "Run Filtered");
            //EDIT END
        }
    }
}
