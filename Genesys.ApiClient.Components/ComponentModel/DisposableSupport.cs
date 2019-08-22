using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Genesys.ApiClient.Components.ComponentModel
{
    public abstract class DisposableSupport : AttributesSupport
    {
        public event EventHandler Disposed;

        public void Dispose()
        {
            // Implements the Basic Dispose Pattern. See http://msdn.microsoft.com/en-us/library/b1yfkh5e%28v=vs.110%29.aspx
            Dispose(true);
            GC.SuppressFinalize(this);

            if (Disposed != null)
                Disposed(this, EventArgs.Empty);
        }

        protected virtual void Dispose(bool disposing) { }
    }
}
