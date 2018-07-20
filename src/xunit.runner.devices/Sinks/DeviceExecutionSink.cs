using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Xunit.Runners.Sinks
{
    class DeviceExecutionSink : TestMessageSink
    {
        readonly SynchronizationContext context;
        readonly ITestListener listener;
        readonly Dictionary<ITestCase, TestCaseViewModel> testCases;

        public DeviceExecutionSink(Dictionary<ITestCase, TestCaseViewModel> testCases,
                                   ITestListener listener,
                                   SynchronizationContext context
        )
        {
            //EDIT BEGIN
            //this.testCases = testCases ?? throw new ArgumentNullException(nameof(testCases));
            //this.listener = listener ?? throw new ArgumentNullException(nameof(listener));
            //this.context = context ?? throw new ArgumentNullException(nameof(context));
            if (testCases == null)
                throw new ArgumentNullException(nameof(testCases));
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            this.testCases = testCases;
            this.listener = listener;
            this.context = context;
            //EDIT END

            Execution.TestFailedEvent += HandleTestFailed;
            Execution.TestPassedEvent += HandleTestPassed;
            Execution.TestSkippedEvent += HandleTestSkipped;
        }

        void HandleTestFailed(MessageHandlerArgs<ITestFailed> args)
        {
            MakeTestResultViewModel(args.Message, TestState.Failed);
        }

        void HandleTestPassed(MessageHandlerArgs<ITestPassed> args)
        {
            MakeTestResultViewModel(args.Message, TestState.Passed);
        }

        void HandleTestSkipped(MessageHandlerArgs<ITestSkipped> args)
        {
            MakeTestResultViewModel(args.Message, TestState.Skipped);
        }

        async void MakeTestResultViewModel(ITestResultMessage testResult, TestState outcome)
        {
            //EDIT BEGIN
#if WINDOWS_APP
            //EDIT TODO
            var tcs = new TaskCompletionSource<TestResultViewModel>(TaskCreationOptions.None);
#else
            var tcs = new TaskCompletionSource<TestResultViewModel>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
            //EDIT END

            //EDIT BEGIN
            //if (!testCases.TryGetValue(testResult.TestCase, out TestCaseViewModel testCase))
            TestCaseViewModel testCase;
            if (!testCases.TryGetValue(testResult.TestCase, out testCase))
            //EDIT END
            {
                // no matching reference, search by Unique ID as a fallback
                testCase = testCases.FirstOrDefault(kvp => kvp.Key.UniqueID?.Equals(testResult.TestCase.UniqueID) ?? false).Value;
                if (testCase == null)
                    return;
            }

            // Create the result VM on the UI thread as it updates properties
            context.Post(_ =>
                         {

                             var result = new TestResultViewModel(testCase, testResult)
                             {
                                 Duration = TimeSpan.FromSeconds((double)testResult.ExecutionTime)
                             };


                             if (outcome == TestState.Failed)
                             {
                                 result.ErrorMessage = ExceptionUtility.CombineMessages((ITestFailed)testResult);
                                 result.ErrorStackTrace = ExceptionUtility.CombineStackTraces((ITestFailed)testResult);
                             }


                             tcs.TrySetResult(result);
                         }, null);


            var r = await tcs.Task;

            listener.RecordResult(r); // bring it back to the threadpool thread
        }

    }
}
