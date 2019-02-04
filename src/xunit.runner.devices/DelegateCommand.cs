using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Xunit.Runners
{
    class DelegateCommand : ICommand
    {
        readonly Func<bool> canExecute;
        readonly Action execute;

        public DelegateCommand(Action execute, Func<bool> canexecute = null)
        {
            //EDIT BEGIN
            //this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));
            this.execute = execute;
            //EDIT END
            canExecute = canexecute ?? (() => true);
        }

        public event EventHandler CanExecuteChanged;

        [DebuggerStepThrough]
        public bool CanExecute(object p)
        {
            try
            {
                return canExecute();
            }
            catch
            {
                return false;
            }
        }

        public void Execute(object p)
        {
            if (!CanExecute(p))
                return;
            execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }


    class DelegateCommand<T> : ICommand
    {
        readonly Func<T, bool> canExecute;
        readonly Action<T> execute;

        public DelegateCommand(Action<T> execute, Func<T, bool> canexecute = null)
        {
            //EDIT BEGIN
            //this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));
            this.execute = execute;
            //EDIT END
            canExecute = canexecute ?? (e => true);
        }

        public event EventHandler CanExecuteChanged;

        [DebuggerStepThrough]
        public bool CanExecute(object p)
        {
            try
            {
                if (p != null && !(p is T))
                {
                    p = (T)Convert.ChangeType(p, typeof(T));
                }
                return canExecute?.Invoke((T)p) ?? true;
            }
            catch
            {
                return false;
            }
        }

        public void Execute(object p)
        {
            if (!CanExecute(p))
                return;

            if (p != null && !(p is T))
            {
                p = (T)Convert.ChangeType(p, typeof(T));
            }
            execute((T)p);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}