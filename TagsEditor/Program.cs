using System;
using System.Windows.Forms;
using System.Linq;
using System.Globalization;

namespace TagsEditor
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            string savedLanguage = Properties.Settings.Default.Language;
            if (!string.IsNullOrEmpty(savedLanguage))
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(savedLanguage);
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TagsEditor()); 
        }
    }
}