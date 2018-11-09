/*

*/

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace ExchangeSharp
{
    public struct SynchronizationContextRemover : INotifyCompletion
    {
        public bool IsCompleted
        {
            get { return SynchronizationContext.Current == null; }
        }

        public void OnCompleted(Action continuation)
        {
            var prevContext = SynchronizationContext.Current;
            if (prevContext == null)
            {
                continuation?.Invoke();
            }
            else
            {
                try
                {
                    SynchronizationContext.SetSynchronizationContext(null);
                    continuation?.Invoke();
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(prevContext);
                }
            }
        }

        public SynchronizationContextRemover GetAwaiter()
        {
            return this;
        }

        public void GetResult()
        {
        }
    }
}
