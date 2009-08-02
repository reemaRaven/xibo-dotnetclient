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
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Net;
using System.IO;

namespace XiboClient
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Check the directories exist
            if (!Directory.Exists(Properties.Settings.Default.LibraryPath) || !Directory.Exists(Properties.Settings.Default.LibraryPath + @"\backgrounds\"))
            {
                //Will handle the create of everything here
                try
                {
                    Directory.CreateDirectory(Properties.Settings.Default.LibraryPath + @"\backgrounds");
                }
                catch (Exception ex)
                { System.Diagnostics.Debug.WriteLine(ex.Message); }
            }

            // Hide the cursor
            Cursor.Position = new Point(ClientSize.Width, ClientSize.Height);
            Cursor.Hide();

            // Change the default Proxy class
            OptionForm.SetGlobalProxy();

            // Create the Schedule
            schedule = new Schedule(Application.UserAppDataPath + "\\" + Properties.Settings.Default.ScheduleFile);

            schedule.ScheduleChangeEvent += new Schedule.ScheduleChangeDelegate(schedule_ScheduleChangeEvent);

            try
            {
                schedule.InitializeComponents();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                MessageBox.Show("Fatal Error initialising the application", "Fatal Error");
                this.Close();
                this.Dispose();
            }
        }

        /// <summary>
        /// Handles the ScheduleChange event
        /// </summary>
        /// <param name="layoutPath"></param>
        void schedule_ScheduleChangeEvent(string layoutPath, int scheduleId, int layoutId)
        {
            System.Diagnostics.Debug.WriteLine(String.Format("Schedule Changing to {0}", layoutPath), "MainForm - ScheduleChangeEvent");

            this.scheduleId = scheduleId;
            this.layoutId = layoutId;

            try
            {
                this.DestroyLayout();

                isExpired = false;

                this.PrepareLayout(layoutPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                isExpired = true;
            }

            // Flush the TraceListener
            System.Diagnostics.Trace.Flush();
        }


        /// <summary>
        /// Prepares the Layout.. rendering all the necessary controls
        /// </summary>
        private void PrepareLayout(string layoutPath)
        {
            XmlLog.AppendStat("Layout Started", StatType.LayoutStart, scheduleId, layoutId, "0");

            // Get this layouts XML
            XmlDocument layoutXml = new XmlDocument();

            // Default or not
            if (layoutPath == Properties.Settings.Default.LibraryPath + @"\Default.xml")
            {
                // We are running with the Default.xml - meaning the schedule doesnt exist
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                Stream resourceStream = assembly.GetManifestResourceStream("XiboClient.Resources.splash.jpg");

                // Load into a stream and then into an Image
                try
                {
                    Image bgSplash = Image.FromStream(resourceStream);

                    Bitmap bmpSplash = new Bitmap(bgSplash, SystemInformation.PrimaryMonitorSize);
                    this.BackgroundImage = bmpSplash;
                }
                catch
                {
                    //Log
                    System.Diagnostics.Debug.WriteLine("Showing Splash Screen");
                }
                return;
            }
            else
            {
                try
                {
                    // try to open the layout file
                    FileStream fs = File.Open(layoutPath, FileMode.Open, FileAccess.Read, FileShare.Write);

                    XmlReader reader = XmlReader.Create(fs);

                    layoutXml.Load(reader);

                    reader.Close();
                    fs.Close();
                }
                catch (Exception ex)
                {
                    // couldnt open the layout file, so use the embedded one
                    System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    Stream resourceStream = assembly.GetManifestResourceStream("XiboClient.Resources.splash.jpg");

                    // Load into a stream and then into an Image
                    try
                    {
                        Image bgSplash = Image.FromStream(resourceStream);

                        Bitmap bmpSplash = new Bitmap(bgSplash, SystemInformation.PrimaryMonitorSize);
                        this.BackgroundImage = bmpSplash;
                    }
                    catch
                    {
                        // Log
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                        System.Diagnostics.Trace.WriteLine("Could not find the layout file {0}", layoutPath);
                    }
                    return;
                }
            }

            // Attributes of the main layout node
            XmlNode layoutNode = layoutXml.SelectSingleNode("/layout");
            
            XmlAttributeCollection layoutAttributes =  layoutNode.Attributes;
            
            // Set the background and size of the form
            layoutWidth = int.Parse(layoutAttributes["width"].Value);
            layoutHeight = int.Parse(layoutAttributes["height"].Value);

            Size clientSize = SystemInformation.PrimaryMonitorSize;

            // Scaling factor, will be applied to all regions
            scaleFactor = Math.Min(clientSize.Width / layoutWidth, clientSize.Height / layoutHeight);
       
            // Want to be able to center this shiv - therefore work out which one of these is going to have left overs
            int backgroundWidth = (int)(layoutWidth * scaleFactor);
            int backgroundHeight = (int)(layoutHeight * scaleFactor);

            double leftOverX;
            double leftOverY;

            try
            {
                leftOverX = Math.Abs(clientSize.Width - backgroundWidth);
                leftOverY = Math.Abs(clientSize.Height - backgroundHeight);

                if (leftOverX != 0) leftOverX = leftOverX / 2;
                if (leftOverY != 0) leftOverY = leftOverY / 2;
            }
            catch 
            {
                leftOverX = 0;
                leftOverY = 0;
            }

            // Are we licensed?
            if (Properties.Settings.Default.licensed == 0)
            {
                // Show a label indicating this fact
                notLic = new Label();
                notLic.Location = new Point(0, 0);
                notLic.Size = new System.Drawing.Size(500, 200);
                notLic.Text = "This Display is not Licensed.";
                notLic.BackColor = Color.WhiteSmoke;
                this.Controls.Add(notLic);
                notLic.BringToFront();
                notLic.Show();
            }

            // New region and region options objects
            regions = new Collection<Region>();
            RegionOptions options = new RegionOptions();

            // Deal with the color
            try
            {
                if (layoutAttributes["bgcolor"].Value != "")
                {
                    this.BackColor = ColorTranslator.FromHtml(layoutAttributes["bgcolor"].Value);
                    options.backgroundColor = layoutAttributes["bgcolor"].Value;
                }
            }
            catch
            {
                this.BackColor = Color.Black; // Default black
                options.backgroundColor = "#000000";
            }

            // Get the background
            try
            {
                string bgFilePath = Properties.Settings.Default.LibraryPath + @"\backgrounds\" + backgroundWidth + "x" + backgroundHeight + "_" + layoutAttributes["background"].Value;

                if (!File.Exists(bgFilePath))
                {
                    Image img = Image.FromFile(Properties.Settings.Default.LibraryPath + @"\" + layoutAttributes["background"].Value);

                    Bitmap bmp = new Bitmap(img, backgroundWidth, backgroundHeight);

                    bmp.Save(bgFilePath, System.Drawing.Imaging.ImageFormat.Jpeg);

                    img.Dispose();
                    bmp.Dispose();
                }
                
                this.BackgroundImage = new Bitmap(bgFilePath);
                options.backgroundImage = bgFilePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);

                // Assume there is no background image
                this.BackgroundImage = null;
                options.backgroundImage = "";
            }

            // Get it to paint the background now
            Application.DoEvents();

            //get the regions
            XmlNodeList listRegions = layoutXml.SelectNodes("/layout/region");

            foreach (XmlNode region in listRegions)
            {
                //each region
                XmlAttributeCollection nodeAttibutes = region.Attributes;

                options.scheduleId = scheduleId;
                options.layoutId = layoutId;
                options.width = (int) (double.Parse(nodeAttibutes["width"].Value) * scaleFactor);
                options.height = (int) (double.Parse(nodeAttibutes["height"].Value) * scaleFactor);
                options.left = (int) (double.Parse(nodeAttibutes["left"].Value) * scaleFactor);
                options.top = (int) (double.Parse(nodeAttibutes["top"].Value) * scaleFactor);
                options.scaleFactor = scaleFactor;

                // Set the backgrounds (used for Web content offsets)
                options.backgroundLeft = options.left * -1;
                options.backgroundTop = options.top * -1;

                //Account for scaling
                options.left = options.left + (int) leftOverX;
                options.top = options.top + (int) leftOverY;
                
                // All the media nodes for this region / layout combination
                options.mediaNodes = region.ChildNodes;

                Region temp = new Region();
                temp.DurationElapsedEvent += new Region.DurationElapsedDelegate(temp_DurationElapsedEvent);

                System.Diagnostics.Debug.WriteLine("Created new region", "MainForm - Prepare Layout");
                temp.regionOptions = options;

                regions.Add(temp);
                this.Controls.Add(temp);

                System.Diagnostics.Debug.WriteLine("Adding region", "MainForm - Prepare Layout");

                Application.DoEvents();
            }

            //Null stuff
            listRegions = null;
        }

        /// <summary>
        /// The duration of a Region has been reached
        /// </summary>
        void temp_DurationElapsedEvent()
        {
            System.Diagnostics.Debug.WriteLine("Region Elapsed", "MainForm - DurationElapsedEvent");

            isExpired = true;
            //check the other regions to see if they are also expired.
            foreach (Region temp in regions)
            {
                if (!temp.hasExpired)
                {
                    isExpired = false;
                }
            }

            if (isExpired && (schedule.ActiveLayouts > 1))
            {
                // Inform each region that the layout containing it has expired
                foreach (Region temp in regions)
                {
                    temp.layoutExpired = true;
                }

                System.Diagnostics.Debug.WriteLine("Region Expired - Next Region.", "MainForm - DurationElapsedEvent");
                schedule.NextLayout();
            }

            Application.DoEvents();
        }

        /// <summary>
        /// Disposes Layout - removes the controls
        /// </summary>
        private void DestroyLayout() 
        {
            System.Diagnostics.Debug.WriteLine("Destroying Layout", "MainForm - DestoryLayout");

            Application.DoEvents();

            if (regions == null) return;

            foreach (Region region in regions)
            {
                this.Controls.Remove(region);

                try
                {
                    System.Diagnostics.Debug.WriteLine("Calling Dispose Region", "MainForm - DestoryLayout");
                    region.Dispose();
                }
                catch (Exception e)
                {
                    //do nothing (perhaps write to some error xml somewhere?)
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

            regions.Clear();
            regions = null;

            this.Controls.Remove(notLic);
            
            // Want a check for powerpoint instances left open by this user
            /*foreach (System.Diagnostics.Process proc in System.Diagnostics.Process.GetProcessesByName("POWERPNT"))
            {
                System.Diagnostics.Debug.WriteLine("Killing leftover Powerpoint process.", "MainForm - DestoryLayout");
                // Close them (End Process)
                proc.Kill();
            }*/
        }

        private Schedule schedule;
        private Collection<Region> regions;
        private bool isExpired = false;
        private Label notLic;
        private int scheduleId;
        private int layoutId;

        double layoutWidth;
        double layoutHeight;
        double scaleFactor;
    }
}