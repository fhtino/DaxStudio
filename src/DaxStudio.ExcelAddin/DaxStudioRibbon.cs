﻿using System;
using Microsoft.Office.Tools.Ribbon;
using Microsoft.Owin.Hosting;
using System.Diagnostics;
using Microsoft.Win32;
using System.Net;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Serilog;

namespace DaxStudio.ExcelAddin
{
    
    public partial class DaxStudioRibbon
    {
        private Process _client;
        private int _port;

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        private void Ribbon1Load(object sender, RibbonUIEventArgs e)
        {
            // look for a running DaxStudio.exe instance and 
            // if we find one try to start a WebHost for it
            var client = DaxStudioStandalone.GetClient();
            var port = 0;
            if (_client == null && client != null)
            {
                _client = client.Process;
                port = client.Port;
            }
            if (port != 0) { WebHost.Start(port); }
        }
        
        private void BtnDaxClick(object sender, RibbonControlEventArgs e)
        {

                
            // Find free port and start the web host
            _port = WebHost.Start();

            //todo - can I search for DaxStudio.exe and set _client if found (would need to send it a message with the port number) ??
            
            // activate DAX Studio if it's already running
            if (_client != null)
            {
                if (!_client.HasExited)
                {
                    SetForegroundWindow(_client.MainWindowHandle);
                    return;
                }
            }
            
            var path = "";
            // try to get path from LocalMachine registry
            path = (string)Registry.LocalMachine.GetValue("Software\\DaxStudio");
            Log.Verbose("{Class} {Method} HKLM Value1: {Value}", "DaxStudioRibbon", "BtnDaxClick", path);
            if (string.IsNullOrWhiteSpace(path))
            {
                path = (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\Software\\DaxStudio", "Path", null);
                Log.Verbose("{Class} {Method} HKLM Value2: {Value}", "DaxStudioRibbon", "BtnDaxClick", path);
            }
            // otherwise get path from HKCU registry
            if (string.IsNullOrWhiteSpace(path))
            {
                path = (string)Registry.GetValue("HKEY_CURRENT_USER\\Software\\DaxStudio", "Path", "");
                Log.Verbose("{Class} {Method} HKCU Value: {Value}", "DaxStudioRibbon", "BtnDaxClick", path);
            }

            Log.Debug("{class} {method} About to launch DaxStudio on path: {path} port: {port}", "DaxStudioRibbon", "BtnDaxClick", path, _port);
            // start Dax Studio process
            _client = Process.Start(new ProcessStartInfo(path, _port.ToString()));
            
        }


        private void DaxStudioRibbon_Close(object sender, EventArgs e)
        {
            // stop the web host
            WebHost.Stop();

            // tell the DaxStudio.exe client to shutdown
            if (_client != null)
            {
                if (!_client.HasExited)
                {
                    SetForegroundWindow(_client.MainWindowHandle);
                    _client.CloseMainWindow();
                }
            }

        }

    }
}