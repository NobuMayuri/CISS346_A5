using System;
using System.IO;
using System.Net.Sockets;
using CryptographyUtilities;
using SMP_Library;

namespace SMPClientConsumer
{
    internal class MessageConsumer
    {
        public static event EventHandler<SMPResponsePacketEventArgs> SMPResponsePacketRecieved;

        public static string publicKeyFile = "consumer_public_key";
        public static string privateKeyFile = "consumer_public_key";

        public static void SendSmpPacket(string serverIpAddress, int port, SmpPacket smpPacket)
        {
            Encryption.GeneratePublicPrivateKeyPair(publicKeyFile, privateKeyFile);
            
            TcpClient client = new TcpClient(serverIpAddress, port);
            NetworkStream networkStream = client.GetStream();
            //CryptNetworkStream cns = new CryptNetworkStream(client, publicKeyFile, privateKeyFile, false);

            //Send the SMP packet
            StreamWriter writer = new StreamWriter(networkStream);
            writer.WriteLine(smpPacket);
            writer.Flush();

            //Receive SMP Response from server
            StreamReader reader = new StreamReader(networkStream);
            string responsePacket = reader.ReadToEnd();

            //Done with the server
            reader.Close();
            writer.Close();

            ProcessSmpResponsePacket(responsePacket);
        }
        private static void ProcessSmpResponsePacket(string responsePacket)
        {
            SMPResponsePacketEventArgs eventArgs = new SMPResponsePacketEventArgs(responsePacket);

            if (SMPResponsePacketRecieved != null) SMPResponsePacketRecieved(null, eventArgs);
        }
    }
}
