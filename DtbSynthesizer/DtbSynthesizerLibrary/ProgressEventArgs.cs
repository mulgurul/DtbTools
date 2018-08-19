using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtbSynthesizerLibrary
{
    public class ProgressEventArgs : EventArgs
    {
        public ProgressEventArgs(int percentage, string message)
        {
            ProgressPercentage = percentage;
            ProgressMessage = message;
            Cancel = false;
        }

        public int ProgressPercentage { get; }

        public string ProgressMessage { get; }

        public bool Cancel { get; set; }
    }
}
