/*
 * Xibo - Digitial Signage - http://www.xibo.org.uk
 * Copyright (C) 2006,2007,2008 Daniel Garner and James Packer
 *
 * This file is part of Xibo.
 *
 * Xibo is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * any later version. 
 *
 * Xibo is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with Xibo.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace XiboClient
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] arg)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            System.Diagnostics.Trace.Listeners.Add(new XiboTraceListener());
            System.Diagnostics.Trace.AutoFlush = true;

            Form formMain;

            if (arg.GetLength(0) > 0)
            {
                System.Diagnostics.Trace.WriteLine("Options Started", "Main");
                formMain = new OptionForm(); 
            }
            else
            {
                System.Diagnostics.Trace.WriteLine("Client Started", "Main");
                formMain = new MainForm();
            }
            
            Application.Run(formMain);

            // Always flush at the end
            System.Diagnostics.Trace.WriteLine("Application Finished", "Main");
            System.Diagnostics.Trace.Flush();
        }
    }

    static class Options
    {
        /// <summary>
        /// The main entry point for the options.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            OptionForm formOptions = new OptionForm();
            Application.Run(formOptions);
        }
    }
}