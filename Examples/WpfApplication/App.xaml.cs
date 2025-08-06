using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using IronPythonConsole;
using PythonConsoleControl;

namespace WpfApplication
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var packagezip = Path.Combine(@"C:\Program Files\SharpCap 4.1 (64 bit)", "PythonLib.zip");
            PythonConfig.SearchPaths = new[] { packagezip, packagezip + "\\site-packages" };
            PythonConsoleWindow w = new PythonConsoleWindow();
            w.ConsoleInitialized += WOnConsoleInitialized;
            MainWindow = w;
            w.Show();
        }

        private void WOnConsoleInitialized(object sender, EventArgs eventArgs)
        {
            //PythonConsoleWindow pcw = (PythonConsoleWindow) sender;
            //pcw.PythonScope.SetVariable("Window", pcw);
            //ScriptSource  script = pcw.PythonScope.Engine.CreateScriptSourceFromString("print 2*3", SourceCodeKind.Statements);
            //script.Execute();
        }
    }
}
