using CryptographyUtilities;
using SMP_Library;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace SMPServer
{
    internal class MessageServer
    {
        public static event EventHandler<PacketEventArgs> PacketRecieved;

        public static string publicKeyFile = "server_public_key";
        public static string privateKeyFile = "server_private_key";

        public static void Start(object o)
        {
            Encryption.GeneratePublicPrivateKeyPair(publicKeyFile, privateKeyFile);

            FormSmpServer form = o as FormSmpServer;

            if (form != null)
            {
                IPAddress iPAddress = IPAddress.Parse(form.IpAddress);
                int port = form.Port;

                TcpListener server = new TcpListener(iPAddress, port);

                server.Start();

                while (true)
                {
                    TcpClient connection = server.AcceptTcpClient();

                    ProcessConnection(connection);
                }
            }
        }

        public static void ProcessConnection(TcpClient connection)
        {
            //NetworkStream networkStream = connection.GetStream();
            CryptNetworkStream cns = new CryptNetworkStream(connection, publicKeyFile, privateKeyFile, true);

            //StreamReader networkStreamReader = new StreamReader(networkStream);

            string version = cns.ReadLine();

            if (version == Enumerations.SmpVersion.Version_2_0.ToString())
            {
                Console.WriteLine("got version: " + version);
                string messageType = cns.ReadLine();
                Console.WriteLine("got message type: " + messageType);

                if (messageType == Enumerations.SmpMessageType.PutMessage.ToString())
                {
                    string priority = cns.ReadLine();
                    string dateTime = cns.ReadLine();
                    string message = cns.ReadLine();
                    string userId = cns.ReadLine();
                    string password = cns.ReadLine();

                    SmpPacket smpPacket = new SmpPacket(version, messageType, priority, dateTime, message, userId, password);

                    ProcessSmpPutPacket(smpPacket);

                    string responsePacket = "Received Packet: " + DateTime.Now + Environment.NewLine;

                    SendSmpResponsePacket(responsePacket, cns);

                    cns.Close();

                    PacketEventArgs eventArgs = new PacketEventArgs(smpPacket);

                    if (PacketRecieved != null) PacketRecieved(null, eventArgs);
                }
                else if (messageType == Enumerations.SmpMessageType.GetMessage.ToString())
                {
                    string priority = cns.ReadLine();
                    string dateTime = cns.ReadLine();
                    string message = cns.ReadLine();
                    string userId = cns.ReadLine();
                    string password = cns.ReadLine();

                    //Console.WriteLine("processed GetMessage priority: " + priority);

                    SmpPacket smpPacket = ProcessSmpGetPacket(priority, userId, password);

                    string record = smpPacket.DateTime + Environment.NewLine;
                    record += smpPacket.Message + Environment.NewLine;

                    string responsePacket = "Message Information: " + Environment.NewLine + record;

                    SendSmpResponsePacket(responsePacket, cns);

                    cns.Close();

                    PacketEventArgs eventArgs = new PacketEventArgs(smpPacket);

                    if (PacketRecieved != null) PacketRecieved(null, eventArgs);
                }
            }
            else
            {
                string responsePacket = "Unsupported Version: " + version + Environment.NewLine;

                SendSmpResponsePacket(responsePacket, cns);

                cns.Close();
            }
        }

        private static void ProcessSmpPutPacket(SmpPacket smpPacket)
        {
            try
            {
                if (smpPacket != null)
                {
                    string record = smpPacket.Version + Environment.NewLine;
                    record += smpPacket.Priority + Environment.NewLine;
                    record += smpPacket.DateTime + Environment.NewLine;
                    record += smpPacket.Message + Environment.NewLine;
                    record += smpPacket.UserId + Environment.NewLine;
                    record += smpPacket.Password + Environment.NewLine;

                    StreamWriter writer = new StreamWriter("Messages.txt", true);

                    writer.WriteLine(record);
                    writer.Flush();

                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogExeption(ex);
            }
        }

        private static SmpPacket ProcessSmpGetPacket(string priority, string userId, string password)
        {
            SmpPacket smpPacket = null;

            try
            {
                StreamReader reader = new StreamReader("Messages.txt");

                string targetSmpVersion = null;
                string targetDateTime = null;
                string targetMessage = null;

                string newFileString = "";
                bool foundTargetPacket = false;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string smpVersion = line;
                    string currPriority = reader.ReadLine();
                    string dateTime = reader.ReadLine();
                    string message = reader.ReadLine();
                    string currUserId = reader.ReadLine();
                    string currPassword = reader.ReadLine();
                    string dummy = reader.ReadLine();

                    if (!foundTargetPacket && priority == currPriority && userId == currUserId && password == currPassword)
                    {
                        foundTargetPacket = true;
                        targetSmpVersion = smpVersion;
                        targetDateTime = dateTime;
                        targetMessage = message;
                    } else
                    {
                        newFileString += smpVersion + Environment.NewLine;
                        newFileString += currPriority + Environment.NewLine;
                        newFileString += dateTime + Environment.NewLine;
                        newFileString += message + Environment.NewLine;
                        newFileString += currUserId + Environment.NewLine;
                        newFileString += currPassword + Environment.NewLine;
                        newFileString += Environment.NewLine;
                    }
                }
                reader.Close();

                if (!foundTargetPacket)
                {
                    return new SmpPacket("", Enumerations.SmpMessageType.GetMessage.ToString(),
                                priority, "", "Error: no message with desired specifications found", userId, password);
                }

                StreamWriter writer = new StreamWriter("Messages.txt", false);
                writer.Write(newFileString);
                writer.Close();

                smpPacket = new SmpPacket(targetSmpVersion, Enumerations.SmpMessageType.GetMessage.ToString(),
                    priority, targetDateTime, targetMessage, userId, password);
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogExeption(ex);
            }

            return smpPacket;
        }
        private static void SendSmpResponsePacket(String responsePacket, CryptNetworkStream cns)
        {
            cns.Write(responsePacket);
        }
    }
}
