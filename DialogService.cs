using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CreateKnxProd
{
    public class DialogService : IDialogService
    {
        public void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }

        public string ChooseFile(string extension, string filter)
        {
            var dlg = new SaveFileDialog();
            dlg.DefaultExt = extension;
            dlg.Filter = filter;

            var result = dlg.ShowDialog();
            if (result != true)
                return null;

            return dlg.FileName;
        }
    }
}
