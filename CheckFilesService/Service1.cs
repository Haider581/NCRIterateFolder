using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Timers;
using System.Xml;
using System.IO;

namespace CheckFilesService
{
    public partial class Service1 : ServiceBase
    {
        Timer timer = new Timer();
        string fileConfigurationsPath = string.Empty;
        double? serviceReloadTimer;
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                WriteToFile(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")
                                .Replace('/', '-') + " " + "Service Started");
                timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
                serviceReloadTimer = ReadAndReturnServiceReloadPathFromSettings();
                if (serviceReloadTimer != null || serviceReloadTimer != 0)
                {
                    timer.Interval = Convert.ToDouble(serviceReloadTimer); //number in milisecinds  
                    timer.Enabled = true;

                    fileConfigurationsPath = ReadAndReturnConfigurationPathFromSettings();
                }

                else
                {
                    WriteToFile(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")
                                    .Replace('/', '-') + " " + "Service Reload Timer cannot be empty or Zero");
                }
            }
            catch (Exception exception)
            {
                WriteToFile(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")
                                                .Replace('/', '-') + " " + exception.Message);
            }

        }

        protected override void OnStop()
        {
            WriteToFile(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")
                                .Replace('/', '-') + " " + "Service Stopped");
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            MakeFileDestinationsList();
        }
        private void MakeFileDestinationsList()
        {
            try
            {
                fileConfigurationsPath = ReadAndReturnConfigurationPathFromSettings();

                var destinationPaths = new List<String>();
                var sourceDestinations = new List<SourceDestination>();

                XmlDocument xmlDoc = new XmlDocument();

                if (!string.IsNullOrEmpty(fileConfigurationsPath))
                {
                    xmlDoc.Load(fileConfigurationsPath);

                    var nodes = xmlDoc.SelectSingleNode("Path/Sources");
                    foreach (XmlNode node in nodes.ChildNodes)
                    {
                        var sourceDestination = new SourceDestination();
                        sourceDestination.SourceAddress = node.FirstChild.InnerText;
                        sourceDestination.Destinations = new List<Destination>();
                        var allDestinations = (XmlNodeList)node.LastChild.ChildNodes;

                        foreach (XmlNode childDestination in allDestinations)
                        {
                            if (string.IsNullOrEmpty(childDestination.ChildNodes[0].InnerText))
                            {
                                WriteToFile(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").Replace('/', '-')
                                                    + " " + "address is a required field");
                                break;
                            }
                            if (childDestination.Attributes != null)
                            {
                                if (string.IsNullOrEmpty(childDestination.Attributes[0].Value))
                                {
                                    WriteToFile(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").Replace('/', '-')
                                                        + " " + "netwrokBased is a required field");
                                    break;
                                }

                                if (childDestination.Attributes[0].Value.Equals("true")
                                    && string.IsNullOrEmpty(childDestination.Attributes[1].Value))
                                {
                                    WriteToFile(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").Replace('/', '-')
                                                        + " " + "authentication is a required field");
                                    break;
                                }

                                if (childDestination.Attributes[0].Value.Equals("true")
                                && childDestination.Attributes[1].Value.Equals("true"))
                                {
                                    if (string.IsNullOrEmpty(childDestination.ChildNodes[1].InnerText))
                                    {
                                        WriteToFile(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").Replace('/', '-')
                                                            + " " + "username is a required field");
                                        break;
                                    }

                                    if (string.IsNullOrEmpty(childDestination.ChildNodes[2].InnerText))
                                    {
                                        WriteToFile(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").Replace('/', '-')
                                                            + " " + "password is a required field");
                                        break;
                                    }
                                }
                            }

                            var destination = new Destination();
                            destination.Address = childDestination.ChildNodes[0].InnerText;
                            if (childDestination.Attributes != null)
                                destination.IsNetworkBasedLocation = childDestination.Attributes[0].Value
                                                == "true" ? true : false;

                            destination.IsAuthenticationRequired = destination.IsNetworkBasedLocation
                                        == true ? childDestination.Attributes[1].Value == "true" ?
                                            true : false : false;
                            destination.Username = destination.IsAuthenticationRequired == true
                                                     ? childDestination.ChildNodes[1].InnerText : null;
                            destination.Password = destination.IsAuthenticationRequired == true
                                                       ? childDestination.ChildNodes[2].InnerText : null;

                            sourceDestination.Destinations.Add(destination);
                        }
                        sourceDestinations.Add(sourceDestination);
                    }

                    MoveFiles(sourceDestinations);
                }

                else
                    WriteToFile(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").Replace('/', '-') + " "
                        + "Settings.xml do not exists " + AppDomain.CurrentDomain.BaseDirectory);
            }
            catch (Exception exception)
            {
                WriteToFile(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").Replace('/', '-') + " " +
                            exception.Message);
            }
        }
        private void MoveFiles(List<SourceDestination> sourceDestinations)
        {
            try
            {
                foreach (var sourceDestination in sourceDestinations)
                {
                    string filesToDelete = @"*";
                    string[] fileList = System.IO.Directory.GetFiles(sourceDestination.SourceAddress, filesToDelete);
                    foreach (string file in fileList)
                    {
                        string fileName = file.Substring(file.LastIndexOf('\\') + 1);
                        int counter = 1;
                        foreach (var destinationPath in sourceDestination.Destinations)
                        {
                            if (counter == sourceDestination.Destinations.Count())
                            {
                                if (!Directory.Exists(destinationPath.Address))
                                    Directory.CreateDirectory(destinationPath.Address);

                                File.Copy(file, destinationPath.Address + fileName, true);
                                WriteToFile(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")
                                            .Replace('/', '-') + " " + fileName + " from "
                                            + sourceDestination.SourceAddress + " has been moved to " + destinationPath.Address);
                                File.Delete(file);
                            }

                            else
                            {
                                if (!Directory.Exists(destinationPath.Address))
                                    Directory.CreateDirectory(destinationPath.Address);

                                File.Copy(file, destinationPath.Address + fileName, true);
                                WriteToFile(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")
                                            .Replace('/', '-') + " " + fileName + " from "
                                            + sourceDestination.SourceAddress + " has been moved to " + destinationPath.Address);
                            }

                            counter++;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                WriteToFile(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").Replace('/', '-') + " " +
                            exception.Message);
            }
        }
        private void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }

        private string ReadAndReturnConfigurationPathFromSettings()
        {
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Settings.xml";
            if (!File.Exists(filepath))
                return null;
            else
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(filepath);
                return xmlDoc.GetElementsByTagName("Fileconfiguration")[0].InnerText;
            }
        }

        private double? ReadAndReturnServiceReloadPathFromSettings()
        {
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Settings.xml";
            if (!File.Exists(filepath))
                return null;
            else
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(filepath);
                return Convert.ToDouble(xmlDoc.GetElementsByTagName("ServiceReloadTimer")
                                        [0].InnerText);
            }
        }
    }
}
