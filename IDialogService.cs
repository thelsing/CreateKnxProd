using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateKnxProd
{
    public interface IDialogService
    {
        void ShowMessage(string message);
        string ChooseFile(string extension, string filter);
    }
}
