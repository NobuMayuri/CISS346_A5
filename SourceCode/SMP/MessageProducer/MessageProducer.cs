using CryptographyUtilities;
using SMP_Library;
using System;
using System.IO;
using System.Net.Sockets;
using System.Windows.Forms;

namespace SMPClientProducer
{
    internal class MessageProducer
    {
        public static event EventHandler<SMPResponsePacketEventArgs> SMPResponsePacketRecieved;

        public static string publicKeyFile = "producer_public_key";
        public static string privateKeyFile = "producer_private_key";

        public static void SendSmpPacket(string serverIpAddress, int port, SmpPacket smpPacket)
        {
            Encryption.GeneratePublicPrivateKeyPair(publicKeyFile, privateKeyFile);

            TcpClient client = new TcpClient(serverIpAddress, port);
            CryptNetworkStream cns = new CryptNetworkStream(client, publicKeyFile, privateKeyFile, false);

            //Send the SMP packet
            cns.Write(smpPacket.ToString());

            //Receive SMP Response from server
            string responsePacket = cns.ReadLine();

            //Done with the server
            cns.Close();

            ProcessSmpResponsePacket(responsePacket);
        }
        private static void ProcessSmpResponsePacket(string responsePacket)
        {
            SMPResponsePacketEventArgs eventArgs = new SMPResponsePacketEventArgs(responsePacket);

            if (SMPResponsePacketRecieved != null) SMPResponsePacketRecieved(null, eventArgs);
        }
    }
}
