using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Management;
using System.Reflection;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.Diagnostics;

namespace Change_IP
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        List<Profile> _allProfiles = new List<Profile>();
        string profilesPath = Application.StartupPath + "/profiles.xml";
        string lastIP = "192.168.1.25";
        private void Button1_Click(object sender, EventArgs e)
        {
            string message = "";
            if (textBox1.Text != "" && textBox2.Text != "")
            {
                message += "IP, Subnet, ";
                SetIP(textBox1.Text, textBox2.Text);
            }
            if (textBox3.Text != "" && comboBox1.Text != ">>Dynamic<<")
            {
                message += "Gateway, ";
                SetGateway(textBox3.Text);
            }
            if (comboBox1.Text != ">>Dynamic<<")
                MessageBox.Show(message.Substring(0, message.Length - 2) + " changed.");
            else
                MessageBox.Show("IP Address changed to dynamic.");
            WriteIPList();
        }

        private string GetSelectedAdapter()
        {
            var adapter = _adapters.Where(w => w.Name == comboBox2.Text).FirstOrDefault();
            return adapter.Id + " " + adapter.Name;
        }

        private List<Adapter> _adapters = new List<Adapter>();

        private List<ManagementObject> GetCollection()
        {
            var objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            var objMOC = objMC.GetInstances();
            var _retVal = new List<ManagementObject>();
            foreach (ManagementObject objMO in objMOC)
                if ((bool)objMO["IPEnabled"])
                    if (GetNetworkName(objMO) != "")
                        _retVal.Add(objMO);
            return _retVal;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            CheckFile();
            var objMOC = GetCollection();
            foreach (var objMO in objMOC)
            {
                string val = GetNetworkName(objMO);
                var id = val.Split(']')[0] + "]";
                var name = val.Substring(val.IndexOf(']') + 1).Trim();
                _adapters.Add(new Adapter
                {
                    Name = name,
                    Id = id
                });
                comboBox2.Items.Add(name);
            }
            comboBox2.SelectedIndex = 0;
            button2.Visible = false;
            if (!File.Exists(profilesPath))
            {
                var fs = File.Create(profilesPath);
                fs.Close();
                File.WriteAllText(profilesPath, "[]");
                _allProfiles = new List<Profile>();
            }
            else
            {
                var xml = GetXMLSettings();
                if (xml != null)
                {
                    var lastIPNode = xml.GetElementsByTagName("LastIP");
                    if (lastIPNode != null)
                    {
                        lastIP = lastIPNode[0].InnerText;
                        button3.Text = "Set " + lastIPNode[0].InnerText;
                    }
                    var profiles = xml.GetElementsByTagName("Profile");
                    if (profiles != null)
                    {
                        for (int i = 0; i < profiles.Count; i++)
                        {
                            try
                            {
                                _allProfiles.Add(new Profile
                                {
                                    Gateway = profiles[i].SelectNodes("Gateway")[0].InnerText,
                                    IPAddress = profiles[i].SelectNodes("IPAddress")[0].InnerText,
                                    Name = profiles[i].SelectNodes("Name")[0].InnerText,
                                    Subnet = profiles[i].SelectNodes("Subnet")[0].InnerText
                                });
                                comboBox1.Items.Add(_allProfiles[_allProfiles.Count - 1].Name);
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                }
            }
            comboBox1.Items.Add(">>New Profile<<");
            comboBox1.Items.Add(">>Dynamic<<");
            comboBox1.SelectedIndex = 0;
            WriteIPList();
            timer1.Interval = 1000;
            timer1.Start();
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available MBytes", true);
        }

        PerformanceCounter cpuCounter;
        PerformanceCounter ramCounter;
        private void GetComputerInfo()
        {
            cpuUsage.Text = "CPU Usage : " + cpuCounter.NextValue().ToString("0.##") + "%";
            ramUsage.Text = "Free RAM : " + ramCounter.NextValue().ToString("0.##") + "MB";
        }
        private XmlDocument GetXMLSettings()
        {
            try
            {
                var xml = new XmlDocument();
                xml.Load(profilesPath);
                return xml;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        private void LblClick(object sender, EventArgs e)
        {
            Clipboard.SetText(((Label)sender).Text);
        }

        private void WriteIPList()
        {
            try
            {
                string strHostName = Dns.GetHostName();
                IPHostEntry iphostentry = Dns.GetHostEntry(strHostName);
                int i = 0;
                panel1.Controls.Clear();
                foreach (IPAddress ipaddress in iphostentry.AddressList)
                {
                    if (!ipaddress.ToString().Contains(":"))
                    {
                        var lbl = new Label
                        {
                            Name = "lbl" + i,
                            Text = ipaddress.ToString(),
                            Location = new Point(15, i * 25 + 25)
                        };
                        lbl.Click += LblClick;
                        panel1.Controls.Add(lbl);
                        i++;
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            button2.Visible = true;
            if (comboBox1.Text == ">>New Profile<<")
            {
                textBox1.Text = "";
                textBox2.Text = "";
                textBox3.Text = "";
            }
            else
            {
                var currentProfile = _allProfiles.Where(w => w.Name == comboBox1.Text).FirstOrDefault();
                if (currentProfile != null)
                {
                    textBox3.Text = currentProfile.Gateway;
                    textBox1.Text = currentProfile.IPAddress;
                    textBox2.Text = currentProfile.Subnet;
                }
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            if (comboBox1.Text == ">>New Profile<<")
            {
                if (textBox1.Text == "")
                {
                    MessageBox.Show("Enter IP Address");
                    return;
                }
                if (textBox2.Text == "")
                {
                    MessageBox.Show("Enter subnet");
                    return;
                }
                var profileNameForm = new newname();
                profileNameForm.ShowDialog();

                _allProfiles.Add(new Profile
                {
                    Gateway = textBox3.Text,
                    IPAddress = textBox1.Text,
                    Name = profileNameForm.profileName,
                    Subnet = textBox2.Text
                });
                comboBox1.Items.Add(profileNameForm.profileName);
            }
            else
            {
                var currentProfile = _allProfiles.Where(w => w.Name == comboBox1.Text).FirstOrDefault();
                if (currentProfile != null)
                {
                    currentProfile.Gateway = textBox3.Text;
                    currentProfile.IPAddress = textBox1.Text;
                    currentProfile.Subnet = textBox2.Text;
                }
            }
            SaveXML();
        }
        private string GetNetworkName(ManagementObject o)
        {
            var props = o.Properties;
            foreach (PropertyData item in props)
                if (item.Name == "Caption")
                    return item.Value.ToString();
            return "";
        }
        public void SetIP(string ip_address, string subnet_mask)
        {
            var objMOC = GetCollection();
            foreach (var objMO in objMOC)
            {
                try
                {
                    if (GetNetworkName(objMO) == GetSelectedAdapter())
                    {
                        var newIP = objMO.GetMethodParameters("EnableStatic");
                        newIP["IPAddress"] = new string[] { ip_address };
                        newIP["SubnetMask"] = new string[] { subnet_mask };
                        if (comboBox1.Text == ">>Dynamic<<")
                            objMO.InvokeMethod("EnableDHCP", null);
                        else
                            objMO.InvokeMethod("EnableStatic", newIP, null);
                    }
                }
                catch (Exception)
                {
                }
            }
        }
        public void SetGateway(string gateway)
        {
            var objMOC = GetCollection();
            foreach (var objMO in objMOC)
            {
                try
                {
                    if (GetNetworkName(objMO) == GetSelectedAdapter())
                    {
                        var newGateway = objMO.GetMethodParameters("SetGateways");
                        newGateway["DefaultIPGateway"] = new string[] { gateway };
                        newGateway["GatewayCostMetric"] = new int[] { 1 };
                        if (comboBox1.Text == ">>Dynamic<<")
                            objMO.InvokeMethod("EnableDHCP", null);
                        else
                            objMO.InvokeMethod("SetGateways", newGateway, null);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            var newIP = lastIP;
            SetIP(newIP, "255.255.255.0");
            MessageBox.Show("IP Address changed to " + lastIP);
            WriteIPList();
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            var current = comboBox1.SelectedIndex;
            comboBox1.Text = ">>Dynamic<<";
            SetIP("", "");
            comboBox1.SelectedIndex = current;
            MessageBox.Show("IP Address changed to dynamic.");
            WriteIPList();
        }
        private void SaveXML()
        {
            var fs = File.Create(profilesPath);
            var xEl = new XElement("Settings",
                new XElement("LastIP", lastIP)
                );
            for (int i = 0; i < _allProfiles.Count; i++)
                xEl.Add(new XElement("Profile",
                             new XElement("Name", _allProfiles[i].Name),
                             new XElement("IPAddress", _allProfiles[i].IPAddress),
                             new XElement("Subnet", _allProfiles[i].Subnet),
                             new XElement("Gateway", _allProfiles[i].Gateway)
                          ));
            new XDocument(
               xEl
                ).Save(fs);
            fs.Close();
        }
        private void CheckFile()
        {
            if (!File.Exists(profilesPath))
            {
                var fs = File.Create(profilesPath);
                new XDocument(
                     new XElement("Settings",
                         new XElement("LastIP", "192.168.1.25"),
                         new XElement("Profile",
                            new XElement("Name", "Can"),
                            new XElement("IPAddress", "192.168.1.152"),
                            new XElement("Subnet", "255.255.255.0"),
                            new XElement("Gateway", "")
                         ),
                         new XElement("Profile",
                            new XElement("Name", "Moxa HSR"),
                            new XElement("IPAddress", "192.168.127.50"),
                            new XElement("Subnet", "255.255.255.0"),
                            new XElement("Gateway", "")
                         ),
                         new XElement("Profile",
                            new XElement("Name", "GR-EP Local"),
                            new XElement("IPAddress", "10.6.34.124"),
                            new XElement("Subnet", "255.255.252.0"),
                            new XElement("Gateway", "10.6.32.1")
                         )
                     )
                    ).Save(fs);
                fs.Close();
            }
        }

        private void panel1_Click(object sender, EventArgs e)
        {
            WriteIPList();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            GetComputerInfo();
        }
    }
    internal class Adapter
    {
        public string Name { get; set; }
        public string Id { get; set; }
    }

    public class Profile
    {
        public string Name { get; set; }
        public string IPAddress { get; set; }
        public string Subnet { get; set; }
        public string Gateway { get; set; }
    }
}
