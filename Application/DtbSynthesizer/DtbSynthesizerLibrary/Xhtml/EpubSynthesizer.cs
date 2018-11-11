using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtbSynthesizerLibrary.Xhtml
{
    public class EpubSynthesizer
    {
        protected bool FireProgress(int percentage, string message)
        {
            var d = Progress;
            if (d != null)
            {
                var a = new ProgressEventArgs(percentage, message);
                d(this, a);
                return a.Cancel;
            }
            return false;
        }

        public event EventHandler<ProgressEventArgs> Progress;


    }
}
