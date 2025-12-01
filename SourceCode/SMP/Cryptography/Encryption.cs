using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace CryptographyUtilities
{
    public class Encryption
    {
        public static string EncryptMessage(string message, string publicKeyFile)
        {
            //Convert the message to an array of bytes
            byte[] messageBytes = Encoding.Unicode.GetBytes(message);

            //Get the RSA public key parameters
            RSAParameters rsaParams = GetRsaParameters(publicKeyFile);

            //Allocate a RSACryptoServiceProvider and import the public key parameters
            RSACryptoServiceProvider cryptoService = new RSACryptoServiceProvider();
            cryptoService.ImportParameters(rsaParams);

            //Encrypt and Decrypt bool Parameter: TRUE: OAEP Padding FALSE: PKCS#1.5 Padding
            byte[] encryptedMessageBytes = cryptoService.Encrypt(messageBytes, false);

            //Save the encrypted message to a file
            string encryptedMessage = Convert.ToBase64String(encryptedMessageBytes);

            return encryptedMessage;
        }
        public static string EncryptMessage(string message, RSAParameters publicKey)
        {
            //Convert the message to an array of bytes
            byte[] messageBytes = Encoding.Unicode.GetBytes(message);

            //Get the RSA public key parameters
            RSAParameters rsaParams = publicKey;

            //Allocate a RSACryptoServiceProvider and import the public key parameters
            RSACryptoServiceProvider cryptoService = new RSACryptoServiceProvider();
            cryptoService.ImportParameters(rsaParams);

            //Encrypt and Decrypt bool Parameter: TRUE: OAEP Padding FALSE: PKCS#1.5 Padding
            byte[] encryptedMessageBytes = cryptoService.Encrypt(messageBytes, false);

            //Save the encrypted message to a file
            string encryptedMessage = Convert.ToBase64String(encryptedMessageBytes);

            return encryptedMessage;
        }
        public static string DecryptMessage(string encryptedMessage, string privateKeyFile)
        {
            byte[] encryptedMessageBytes = Convert.FromBase64String(encryptedMessage);

            //Get the RSA public key parameters
            RSAParameters rsaParams = GetRsaParameters(privateKeyFile);

            //Allocate a RSACryptoServiceProvider and import the private key parameters
            RSACryptoServiceProvider cryptoService = new RSACryptoServiceProvider();
            cryptoService.ImportParameters(rsaParams);

            //Encrypt and Decrypt bool Parameter: TRUE: OAEP Padding FALSE: PKCS#1.5 Padding
            byte[] decryptedMessageBytes = cryptoService.Decrypt(encryptedMessageBytes, false);

            //Convert bytes to string
            string decryptedMessage = Encoding.Unicode.GetString(decryptedMessageBytes);

            return decryptedMessage;
        }
        public static string EncryptMessageToFile(string message, string publicKeyFile, string encryptedMessageFile)
        {
            string encryptedMessage = EncryptMessage(message, publicKeyFile);

            File.WriteAllText(encryptedMessageFile, encryptedMessage);

            return encryptedMessage;
        }
        public static string DecryptMessageFromFile(string encryptedMessageFile, string privateKeyFile)
        {
            string encryptedMessage = File.ReadAllText(encryptedMessageFile);
 
            string decryptedMessage = DecryptMessage(encryptedMessage, privateKeyFile);

            return decryptedMessage;
        }
        public static RSAParameters GetRsaParameters(string keyFile)
        {
            //Read the key from the file
            string keyString = File.ReadAllText(keyFile);

            //Convert the key string to a RSAParameters object
            StringReader stringReader = new StringReader(keyString);
            XmlSerializer serializer = new XmlSerializer(typeof(RSAParameters));
            RSAParameters key = (RSAParameters)serializer.Deserialize(stringReader);

            return key;
        }

        public static RSAParameters GetRsaParametersFromXml(string keyString)
        {
            StringReader stringReader = new StringReader(keyString);
            XmlSerializer serializer = new XmlSerializer(typeof(RSAParameters));
            RSAParameters key = (RSAParameters)serializer.Deserialize(stringReader);

            return key;
        }

        public static string GetXmlStringFromKey(RSAParameters key)
        {
            StringWriter stringWriter = new StringWriter();
            XmlSerializer serializer = new XmlSerializer(typeof(RSAParameters));
            serializer.Serialize(stringWriter, key);
            string keyString = stringWriter.ToString();

            return keyString;
        }

        public static string StringToB64(string s)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
        }
        public static string B64ToString(string s)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(s));
        }

        public static void GeneratePublicPrivateKeyPair(string publicKeyFile, string privateKeyFile)
        {
            if (File.Exists(publicKeyFile))
            {
                return;
            }

            //RSACryptoServiceProvider: 2048-bit RSA Public/Private Key Pair
            RSACryptoServiceProvider cryptoService = new RSACryptoServiceProvider(2048);

            //Generate the public key
            RSAParameters publicKey = cryptoService.ExportParameters(false);

            //Convert the public key into a string
            StringWriter stringWriter = new StringWriter();
            XmlSerializer serializer = new XmlSerializer(typeof(RSAParameters));
            serializer.Serialize(stringWriter, publicKey);
            string publicKeyString = stringWriter.ToString();

            //Save the public key to a file
            File.WriteAllText(publicKeyFile, publicKeyString);

            //Generate the private key
            RSAParameters privateKey = cryptoService.ExportParameters(true);

            //Convert the private key into a string
            stringWriter = new StringWriter();
            serializer = new XmlSerializer(typeof(RSAParameters));
            serializer.Serialize(stringWriter, privateKey);
            string privateKeyString = stringWriter.ToString();

            //Save the private key to a file
            File.WriteAllText(privateKeyFile, privateKeyString);
        }
    }

    public class CryptNetworkStream
    {
        NetworkStream ns;
        StreamWriter writer;
        StreamReader reader;
        RSAParameters clientPublicKey;
        string myPrivateKeyFilename;
        Queue<string> lineQueue;

        public CryptNetworkStream(TcpClient client, string myPublicKeyFilename, string myPrivateKeyFilename, bool isServer) {
            ns = client.GetStream();
            writer = new StreamWriter(ns) { AutoFlush = true };
            reader = new StreamReader(ns);

            lineQueue = new Queue<string>();

            this.myPrivateKeyFilename = myPrivateKeyFilename;

            RSAParameters myPublicKey = Encryption.GetRsaParameters(myPublicKeyFilename);
            string myPublicKeyXml = Encryption.GetXmlStringFromKey(myPublicKey);

            string clientPublicKeyXml;
            if (isServer)
            {
                writer.WriteLine(Encryption.StringToB64(myPublicKeyXml));
                clientPublicKeyXml = Encryption.B64ToString(reader.ReadLine());
            } else
            {
                clientPublicKeyXml = Encryption.B64ToString(reader.ReadLine());
                writer.WriteLine(Encryption.StringToB64(myPublicKeyXml));
            }

            clientPublicKey = Encryption.GetRsaParametersFromXml(clientPublicKeyXml);
        }

        public string ReadLine()
        {
            if (lineQueue.Count > 0)
            {
                return lineQueue.Dequeue();
            }

            string encryptedInput = Encryption.B64ToString(reader.ReadLine());
            string decryptedInput = Encryption.DecryptMessage(encryptedInput, myPrivateKeyFilename);

            string[] lines = decryptedInput.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            
            for (int i = 0; i < lines.Length; i++)
            {
                lineQueue.Enqueue(lines[i]);
            }

            return lineQueue.Dequeue();
        }
        public string ReadToEnd()
        {
            string result = "";
            while (lineQueue.Count > 0)
            {
                result += lineQueue.Dequeue() + Environment.NewLine;
            }

            string encryptedInput = Encryption.B64ToString(reader.ReadToEnd());
            string decryptedInput = Encryption.DecryptMessage(encryptedInput, myPrivateKeyFilename);
            result = result + decryptedInput;

            return result;
        }
        public void Write(string input)
        {
            string encryptedInput = Encryption.StringToB64(Encryption.EncryptMessage(input, clientPublicKey));
            writer.WriteLine(encryptedInput);
        }

        public void Close()
        {
            reader.Close();
            writer.Close();
        }
    }
}
