// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using ICSharpCode.AvalonEdit;
using System.Windows.Media;
using PythonConsoleControl;

namespace IronPythonConsole
{
    public class ConsoleOptions
    {
        TextEditor _textEditor;
        PythonConsolePad _pad;
        
        public ConsoleOptions(PythonConsolePad pad)
        {
            _textEditor = pad.Control;
            _pad = pad;
        }

        [DefaultValue(false)]
        public bool ShowSpaces
        {
            get { return _textEditor.TextArea.Options.ShowSpaces; }
            set { _textEditor.TextArea.Options.ShowSpaces = value; }
        }

        [DefaultValue(false)]
        public bool ShowTabs
        {
            get { return _textEditor.TextArea.Options.ShowTabs; }
            set { _textEditor.TextArea.Options.ShowTabs = value; }
        }

        [DefaultValue(false)]
        public bool AllowScrollBelowDocument
        {
            get { return _textEditor.TextArea.Options.AllowScrollBelowDocument; }
            set { _textEditor.TextArea.Options.AllowScrollBelowDocument = value; }
        }

        [DefaultValue("Consolas")]
        public string FontFamily
        {
            get { return _textEditor.TextArea.FontFamily.ToString(); }
            set { _textEditor.TextArea.FontFamily = new FontFamily(value); }
        }

        [DefaultValue(12.0)]
        public double FontSize
        {
            get { return _textEditor.TextArea.FontSize; }
            set { _textEditor.TextArea.FontSize = value; }
        }

        [DefaultValue(true)]
        public bool FullAutocompletion
        {
            get { return _pad.Console?.AllowFullAutocompletion ?? false; }
            set { _pad.Console.AllowFullAutocompletion = value; }
        }

        [DefaultValue(false)]
        public bool CtrlSpaceAutocompletion
        {
            get { return _pad.Console?.AllowCtrlSpaceAutocompletion ?? false; }
            set { _pad.Console.AllowCtrlSpaceAutocompletion = value; }
        }

        [DefaultValue(true)]
        public bool DisableAutocompletionForCallables
        {
            get { return _pad.Console?.DisableAutocompletionForCallables ?? false; }
            set { _pad.Console.DisableAutocompletionForCallables = value; }
        }
    }
}
