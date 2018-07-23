using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Xunit.Runners.Pages;

namespace Xunit.Runners
{
    class Navigator : INavigation
    {
        readonly Frame frame;

        public Navigator(Frame frame)
        {
            this.frame = frame;
            //EDIT BEGIN
#if WINDOWS_APP
            //to allow back navigation
            TestCaseViewModel.Navigation = this;
            TestAssemblyViewModel.Navigation = this;
            TestResultViewModel.Navigation = this;
#endif
            //EDIT END
        }

        public Task NavigateTo(NavigationPage page, object dataContext = null)
        {
            Type t;
            switch (page)
            {
                case NavigationPage.Home:
                    t = typeof(HomePage);
                    break;
                case NavigationPage.AssemblyTestList:
                    t = typeof(AssemblyTestListPage);
                    break;
                case NavigationPage.TestResult:
                    t = typeof(TestResultPage);
                    break;
                case NavigationPage.Credits:
                    t = typeof(CreditsPage);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            frame.Navigate(t, dataContext);

            //EDIT BEGIN
            //return Task.CompletedTask;
            return Task.FromResult(0);
            //EDIT END
        }

        //EDIT BEGIN
#if WINDOWS_APP
        public void Back()
        {
            if (frame.CanGoBack)
                frame.GoBack();
        }
#endif
        //EDIT END
    }
}
