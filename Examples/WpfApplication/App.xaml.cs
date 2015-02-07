using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using IronPythonConsole;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

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
