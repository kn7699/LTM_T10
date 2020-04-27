﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace Server
{
    //The commands for interaction between the server and the client
    enum Command
    {
        Login,      //dang nhap vao server
        Logout,     //??ng xu?t
        Message,    // gui tin nhan toi cac client
        List,       //Get a list of users in the chat room from the server
        Null        //No command
    }

    public partial class SGSserverForm : Form
    {
        // Khai báo thông tin của mỗi client gồm socket và tên
        struct ClientInfo
        {
            public EndPoint endpoint;   //socket của client
            public string strName;      // Tên của mỗi người tham gia phòng chat
        }

        // thu thập tất cả client vào mảng client
        ArrayList clientList;

        //Socket chính nghe từ mỗi client
        Socket serverSocket;

        byte[] byteData = new byte[1024];

        public SGSserverForm()
        {
            clientList = new ArrayList();     //Khai báo mảng client
            InitializeComponent();
        }

    private void Form1_Load(object sender, EventArgs e)
    {            
        try
        {
	    CheckForIllegalCrossThreadCalls = false;

            // Giao thức UDP
            serverSocket = new Socket(AddressFamily.InterNetwork, 
                SocketType.Dgram, ProtocolType.Udp);

            //Assign the any IP of the machine and listen on port number 1000
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 1000);

            //Kết nối địa chỉ IP với Socket
            serverSocket.Bind(ipEndPoint);
            
            IPEndPoint ipeSender = new IPEndPoint(IPAddress.Any, 0);
           
            EndPoint epSender = (EndPoint) ipeSender;

            // Bắt đầu nhận dữ liệu
            serverSocket.BeginReceiveFrom (byteData, 0, byteData.Length, 
                SocketFlags.None, ref epSender, new AsyncCallback(OnReceive), epSender);                
        }
        catch (Exception ex) 
        { 
            MessageBox.Show(ex.Message, "SGSServerUDP", 
                MessageBoxButtons.OK, MessageBoxIcon.Error); 
        }            
    }
        int sodem = 0;
        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                IPEndPoint ipeSender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint epSender = (EndPoint)ipeSender;

                serverSocket.EndReceiveFrom (ar, ref epSender);

                var diachiip = epSender.ToString().Split(':')[0];
                var portlog = epSender.ToString().Split(':')[1];


                // Chuyển mảng byte người nhận
                Data msgReceived = new Data(byteData);

                // Gửi tin nhắn
                Data msgToSend = new Data();

                byte [] message;
                
                //If the message is to login, logout, or simple text message
                //then when send to others the type of the message remains the same
                msgToSend.cmdCommand = msgReceived.cmdCommand;
                msgToSend.strName = msgReceived.strName;

                switch (msgReceived.cmdCommand)
                {
                    case Command.Login:
                        
                        //Khi một người đăng nhập thì add người đó vào danh sách các client

                        ClientInfo clientInfo = new ClientInfo();
                        clientInfo.endpoint = epSender;      
                        clientInfo.strName = msgReceived.strName;

                        // Code check trùng ip tên sever
                        string content = "";
                        var chuoiip = clientInfo.endpoint.ToString().Split(':')[0];
                        var chuoiport = clientInfo.endpoint.ToString().Split(':')[1];
                        if (clientList.Count != 0 && clientList.Count <= 10)
                        {
                            foreach (ClientInfo client in clientList)
                            {
                                var ipclient = client.endpoint.ToString().Split(':')[0];
                                if (ipclient != chuoiip)
                                {
                                    clientList.Add(clientInfo);
                                    msgToSend.strMessage = "<<<" + diachiip + " đã tham gia phòng chat>>>";
                                    content = DateTime.Now.ToString("HH:mm dd/MM/yyyy") + "- người dùng có địa chỉ ip là: " + chuoiip + ", port: "+ chuoiport + " đã kết nối vào cuộc trò chuyện";
                                }
                                else
                                {
                                    msgToSend.strMessage = "-- Đã chặn người dùng : " + clientInfo.strName + " do trùng IP : " + diachiip + "--";
                                }
                            }
                        }
                        else if(clientList.Count > 10)
                        {
                            msgToSend.strMessage = "-- Đã chặn người dùng : " + clientInfo.strName + " do hệ thống giới hạn lượt truy cập là: 10 người --";
                            content = DateTime.Now.ToString("HH:mm dd/MM/yyyy") + "- người dùng có địa chỉ ip là: " + chuoiip + ", port: " + chuoiport + " đã bị chặn kết nối do quá 10 kết nối tới server";
                        }        
                        else
                        {
                            clientList.Add(clientInfo);
                            msgToSend.strMessage = "<<<" + diachiip + " đã tham gia phòng chat>>>";
                            content = DateTime.Now.ToString("HH:mm dd/MM/yyyy") + "- người dùng có địa chỉ ip là: " + chuoiip + ", port: " + chuoiport + " đã kết nối vào cuộc trò chuyện";
                        }
                        // hêts
                        // Lưu lịch sử vào file

                        string path = @"D:\\access.log";
                        if (!System.IO.File.Exists(path))
                        {
                            using (StreamWriter sw = System.IO.File.CreateText(path))
                            {
                                sw.WriteLine(content);
                                sw.Flush();
                                sw.Close();
                            }
                        }
                        else
                        {
                            using (StreamWriter sw = new StreamWriter(path, true))
                            {
                                sw.WriteLine(content);
                                sw.Flush();
                                sw.Close();
                            }
                        }


                        //Gửi tin nhắn tới tất cả các client đã tham gia phòng

                        break;

                    case Command.Logout:                    
                        
                        //Khi một người sử dụng muốn đăng xuất thì server sẽ tìm người đó trong danh sách các client và đóng kết nối của client đó

                        int nIndex = 0;
                        foreach (ClientInfo client in clientList)
                        {
                            if (client.endpoint == epSender)
                            {
                                clientList.RemoveAt(nIndex);
                                break;
                            }
                            ++nIndex;
                        }                                               
                        
                        msgToSend.strMessage = "<<<" + diachiip + " đã rời phòng chat>>>";
                        // Lưu lịch sử vào file
                        var content1 = DateTime.Now.ToString("HH:mm dd/MM/yyyy") + "- người dùng có địa chỉ ip là: " + diachiip + ", port: " + portlog+ " đã ngắt kết nối tới cuộc trò chuyện";
                        string path1 = @"D:\\access.log";
                        if (!System.IO.File.Exists(path1))
                        {
                            using (StreamWriter sw = System.IO.File.CreateText(path1))
                            {
                                sw.WriteLine(content1);
                                sw.Flush();
                                sw.Close();
                            }
                        }
                        else
                        {
                            using (StreamWriter sw = new StreamWriter(path1, true))
                            {
                                sw.WriteLine(content1);
                                sw.Flush();
                                sw.Close();
                            }
                        }

                        break;

                    case Command.Message:

                        //cài đặt kí tự khi gửi
                        msgToSend.strMessage = msgReceived.strName + ": " + msgReceived.strMessage;
                        break;

                    case Command.List:

                        //Gửi tên của tất cả các client đến client mới
                        msgToSend.cmdCommand = Command.List;
                        msgToSend.strName = null;
                        msgToSend.strMessage = null;

                        // lên danh sách tên client
                        foreach (ClientInfo client in clientList)
                        {
                            msgToSend.strMessage += client.strName + "*";   
                        }                        

                        message = msgToSend.ToByte();

                        //gửi lên phòng chat
                        serverSocket.BeginSendTo (message, 0, message.Length, SocketFlags.None, epSender, 
                                new AsyncCallback(OnSend), epSender);                        
                        break;
                }

                if (msgToSend.cmdCommand != Command.List)  
                {
                    message = msgToSend.ToByte();

                    foreach (ClientInfo clientInfo in clientList)
                    {
                        if (clientInfo.endpoint != epSender ||
                            msgToSend.cmdCommand != Command.Login)
                        {
                            //Gửi tin nhắn tới tất cả mọi client
                            serverSocket.BeginSendTo (message, 0, message.Length, SocketFlags.None, clientInfo.endpoint, 
                                new AsyncCallback(OnSend), clientInfo.endpoint);                           
                        }
                    }

                    txtLog.Text += msgToSend.strMessage + "\r\n";
                }

                //phản hồi từ client khi muốn đăng xuât
                if (msgReceived.cmdCommand != Command.Logout)
                {
                  
                    serverSocket.BeginReceiveFrom (byteData, 0, byteData.Length, SocketFlags.None, ref epSender, 
                        new AsyncCallback(OnReceive), epSender);
                }
            }
            catch (Exception ex)
            { 
                MessageBox.Show(ex.Message, "SGSServerUDP", MessageBoxButtons.OK, MessageBoxIcon.Error); 
            }
        }

        public void OnSend(IAsyncResult ar)
        {
            try
            {                
                serverSocket.EndSend(ar);
            }
            catch (Exception ex)
            { 
                MessageBox.Show(ex.Message, "SGSServerUDP", MessageBoxButtons.OK, MessageBoxIcon.Error); 
            }
        }

        private void txtLog_TextChanged(object sender, EventArgs e)
        {

        }

        private void SGSserverForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if(sodem == 0)
            {
                var content1 = DateTime.Now.ToString("HH:mm dd/MM/yyyy") + "- Sever đã bị tắt đi ";
                string path1 = @"D:\\access.log";
                if (!System.IO.File.Exists(path1))
                {
                    using (StreamWriter sw = System.IO.File.CreateText(path1))
                    {
                        sw.WriteLine(content1);
                        sw.Flush();
                        sw.Close();
                    }
                }
                else
                {
                    using (StreamWriter sw = new StreamWriter(path1, true))
                    {
                        sw.WriteLine(content1);
                        sw.Flush();
                        sw.Close();
                    }
                }
                sodem++;
            }    
            
            this.Close();
        }
    }

    //The data structure by which the server and the client interact with 
    //each other
    class Data
    {
        //Default constructor
        public Data()
        {
            this.cmdCommand = Command.Null;
            this.strMessage = null;
            this.strName = null;
        }

        //Chuyển đổi byte thành các dạng data
        public Data(byte[] data)
        {
            //The first four bytes are for the Command
            this.cmdCommand = (Command)BitConverter.ToInt32(data, 0);

            //The next four store the length of the name
            int nameLen = BitConverter.ToInt32(data, 4);

            //The next four store the length of the message
            int msgLen = BitConverter.ToInt32(data, 8);

            //This check makes sure that strName has been passed in the array of bytes
            if (nameLen > 0)
                this.strName = Encoding.UTF8.GetString(data, 12, nameLen);
            else
                this.strName = null;

            //This checks for a null message field
            if (msgLen > 0)
                this.strMessage = Encoding.UTF8.GetString(data, 12 + nameLen, msgLen);
            else
                this.strMessage = null;
        }

        //Converts the Data structure into an array of bytes
        public byte[] ToByte()
        {
            List<byte> result = new List<byte>();

            //First four are for the Command
            result.AddRange(BitConverter.GetBytes((int)cmdCommand));

            //Add the length of the name
            if (strName != null)
                result.AddRange(BitConverter.GetBytes(strName.Length));
            else
                result.AddRange(BitConverter.GetBytes(0));

            //Length of the message
            if (strMessage != null)
                result.AddRange(BitConverter.GetBytes(strMessage.Length));
            else
                result.AddRange(BitConverter.GetBytes(0));

            //Add the name
            if (strName != null)
                result.AddRange(Encoding.UTF8.GetBytes(strName));

            //And, lastly we add the message text to our array of bytes
            if (strMessage != null)
                result.AddRange(Encoding.UTF8.GetBytes(strMessage));

            return result.ToArray();
        }

        public string strName;      //Name by which the client logs into the room
        public string strMessage;   //Message text
        public Command cmdCommand;  //Command type (login, logout, send message, etcetera)
    }
}