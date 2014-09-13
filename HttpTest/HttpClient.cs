using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Collections.Specialized;
using System.Net.Sockets;
using System.IO;

namespace HttpTest
{


    public class HttpClient
    {

        public class Response
        {
            public int StatusCode { get; set; }
            public string StatusDescription { get; set; }
            public string Scheme { get; set; }


            NameValueCollection headers;
            public NameValueCollection Headers
            {
                get { return headers; }
            }

            public byte[] BodyData { get; set; }


            public Response()
            {
                headers = new NameValueCollection();
            }
        }

        int bufferSize = 128;
        public int BufferSize
        {
            get { return bufferSize; }
            set { bufferSize = value; }
        }


        int receiveTimeout = 10 * 1000;
        public int ReceiveTimeout
        {
            get { return receiveTimeout; }
            set { receiveTimeout = value; }
        }


        int sendTimeout = 3 * 1000;
        public int SendTimeout
        {
            get { return sendTimeout; }
            set { sendTimeout = value; }
        }

        bool HasSign(byte[] data, byte[] sign)
        {
            if (data.Length >= sign.Length)
            {
                bool has = true;

                for (int i = 1; i <= sign.Length; i++)
                {
                    has = has && data[data.Length - i] == sign[sign.Length - i];
                }

                return has;
            }

            return false;
        }

        const string HEADER_SPLIT_SIGN = ": ";
        public Response GetHttpData(IPEndPoint address, byte[] headersData)
        {
            Response response = null;

            Console.WriteLine(1);

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            socket.ReceiveTimeout = receiveTimeout;
            socket.SendTimeout = sendTimeout;
            socket.ReceiveBufferSize = this.bufferSize;
            socket.SendBufferSize = this.bufferSize;
            socket.Connect(address.Address, address.Port);


            Console.WriteLine(2);


            if (socket.Connected)
            {
                socket.Send(headersData);

                using (NetworkStream netStream = new NetworkStream(socket, FileAccess.ReadWrite))
                {
                    /*
                    if (netStream.CanWrite)
                    {
                        netStream.Write(headersData, 0, headersData.Length);

                        netStream.Flush();
                    }
                    */
                    Console.WriteLine(3);

                    if (netStream.CanRead)
                    {
                        response = new Response();

                        using (BinaryReader reader = new BinaryReader(netStream))
                        {
                            Console.WriteLine(4);


                            //解析出头数据
                            using (MemoryStream headerStream = new MemoryStream())
                            {

                                byte[] stopSign = Encoding.ASCII.GetBytes("\r\n\r\n");

                                int stopSignSize = stopSign.Length;
                                int signSize = stopSignSize * 2;
                                int byteIndex = stopSignSize - 1;

                                byte[] sign = new byte[signSize];


                                int n = 0;
                                while (true)
                                {
                                    //Console.WriteLine(4);
                                    byte b = reader.ReadByte();

                                    for (int i = 0; i < signSize - 1; i++)
                                    {
                                        sign[i] = sign[i + 1];
                                    }

                                    sign[signSize - 1] = b;

                                    n++;

                                    if (n > stopSignSize)
                                    {
                                        headerStream.WriteByte(sign[byteIndex]);
                                    }

                                    if (HasSign(sign, stopSign))
                                    {
                                        break;
                                    }
                                }
                                Console.WriteLine(5);
                                headerStream.Seek(0, SeekOrigin.Begin);

                                using (StreamReader streamReader = new StreamReader(headerStream, true))
                                {

                                    bool firstLine = true;

                                    while (true)
                                    {
                                        string line = streamReader.ReadLine();

                                        if (string.IsNullOrEmpty(line))
                                        {
                                            break;
                                        }

                                        if (!firstLine)
                                        {
                                            int signIndex = line.IndexOf(HEADER_SPLIT_SIGN);

                                            int valueIndex = signIndex + HEADER_SPLIT_SIGN.Length;

                                            if (signIndex > 0 && valueIndex < line.Length)
                                            {
                                                string key = line.Substring(0, signIndex);
                                                string value = line.Substring(valueIndex);
                                                response.Headers[key] = value;

                                            }
                                        }
                                        else
                                        {
                                            if (!string.IsNullOrEmpty(line))
                                            {
                                                int inx_space1 = line.IndexOf(' ');
                                                int inx_space2 = line.IndexOf(' ', inx_space1 + 1);
                                                if (inx_space2 > inx_space1 && inx_space1 > 0)
                                                {
                                                    string codeString = line.Substring(inx_space1 + 1, inx_space2 - inx_space1);
                                                    int code = 0;
                                                    if (int.TryParse(codeString, out code))
                                                    {
                                                        response.StatusCode = code;
                                                    }
                                                    response.Scheme = line.Substring(0, inx_space1);
                                                    response.StatusDescription = line.Substring(inx_space2 + 1);
                                                }
                                            }

                                            firstLine = false;
                                        }
                                    }

                                    streamReader.Close();
                                    streamReader.Dispose();
                                }
                                Console.WriteLine(6);

                                headerStream.Close();
                                headerStream.Dispose();
                            }

                            Console.WriteLine(7);
                            //解析Body 数据

                            if (response.Headers["Content-Length"] != null)
                            {
                                int contentLength = int.Parse(response.Headers["Content-Length"]);


                                using (var bodyStream = new MemoryStream(contentLength))
                                {
                                    byte[] buffer = new byte[this.bufferSize];
                                    int readCount;

                                    while ((readCount = netStream.Read(buffer, 0, this.bufferSize)) != 0)
                                    {
                                        bodyStream.Write(buffer, 0, readCount);
                                    }

                                    response.BodyData = bodyStream.ToArray();

                                    bodyStream.Close();
                                    bodyStream.Dispose();
                                }


                            }
                            else if (response.Headers["Transfer-Encoding"] == "chunked")
                            {
                                using (var bodyStream = new MemoryStream())
                                {


                                    byte[] stopSign = Encoding.ASCII.GetBytes("\r\n");

                                    int stopSignSize = stopSign.Length;
                                    int signSize = stopSignSize * 2;

                                    int byteIndex = stopSignSize - 1;

                                    int chunkedLenth = 0;

                                    StringBuilder __sb = new StringBuilder();


                                    while (netStream.DataAvailable)
                                    {
                                        /*
                                         * 5353 rn
                                         * 
                                         * 00 05    1
                                         * 00 53    2
                                         * 05 35    3
                                         * 53 53    4
                                         * 35 3r    5
                                         * 53 rn    6
                                     
                                     
                                         */

                                        byte[] sign = new byte[signSize];
                                        List<byte> bytes = new List<byte>();

                                        int n = 0;

                                        while (netStream.DataAvailable)
                                        {


                                            byte b = reader.ReadByte();


                                            __sb.Append(b);

                                            for (int i = 0; i < signSize - 1; i++)
                                            {
                                                sign[i] = sign[i + 1];
                                            }

                                            sign[signSize - 1] = b;


                                            n++;

                                            if (n > stopSignSize)
                                            {
                                                bytes.Add(sign[byteIndex]);
                                            }

                                            if (HasSign(sign, stopSign))
                                            {
                                                break;
                                            }
                                        }

                                        if (bytes.Count == 0)
                                        {
                                            break;
                                        }

                                        try
                                        {
                                            string hexString = Encoding.ASCII.GetString(bytes.ToArray());




                                            chunkedLenth = Convert.ToInt32(hexString, 16);

                                            if (chunkedLenth == 0)
                                            {
                                                break;
                                            }
                                            else
                                            {
                                                int totalCount = 0;

                                                byte[] buffer = new byte[bufferSize];
                                                while (totalCount < chunkedLenth && netStream.DataAvailable)
                                                {


                                                    int readCount = reader.Read(buffer, 0, bufferSize);

                                                    if (readCount != 0)
                                                    {
                                                        __sb.Append(buffer);
                                                        totalCount += readCount;
                                                        bodyStream.Write(buffer, 0, readCount);
                                                    }
                                                    else
                                                    {
                                                        break;
                                                    }
                                                }

                                            }

                                            response.BodyData = bodyStream.ToArray();

                                        }
                                        catch (Exception exs)
                                        {

                                            Console.WriteLine(__sb.ToString());

                                            throw exs;

                                        }

                                    }
                                    Console.WriteLine(8);

                                    bodyStream.Close();
                                    bodyStream.Dispose();

                                }
                            }
                            else
                            {
                                Console.WriteLine(9);


                                using (var bodyStream = new MemoryStream())
                                {
                                    byte[] buffer = new byte[this.bufferSize];
                                    int readCount;
                                    while ((readCount = reader.Read(buffer, 0, this.bufferSize)) != 0)
                                    {
                                        bodyStream.Write(buffer, 0, readCount);
                                    }

                                    response.BodyData = bodyStream.ToArray();

                                    bodyStream.Close();
                                    bodyStream.Dispose();
                                }
                            }


                            reader.Close();
                            reader.Dispose();
                        }

                    }

                    netStream.Close();
                    netStream.Dispose();
                }
            }

            if (socket != null)
            {
                Console.WriteLine(10);
                // socket.Disconnect(true);
                socket.Close();
                socket.Dispose();

            }
            Console.WriteLine(11);

            /*
            try
            {


            }
            catch (Exception ex)
            {

            }
            finally
            {
               
            }
          */
            return response;
        }


    }
}
