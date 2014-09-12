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


            public Stream BodyStream { get; set; }

            public Response()
            {
                headers = new NameValueCollection();
            }
        }

        public int bufferSize = 256;
        public int BufferSize
        {
            get { return bufferSize; }
            set { bufferSize = value; }
        }

        bool HasSign(byte[] data, byte[] sign)
        {

            bool has = true;

            for (int i = 1; i <= sign.Length; i++)
            {
                has = has && data[data.Length - i] == sign[sign.Length - i];
            }

            return has;
        }

        const string HEADER_SPLIT_SIGN = ": ";
        public Response GetHttpData(IPEndPoint address, byte[] headersData)
        {
            Response response = null;

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);           
     
            try
            {

                socket.Connect(address.Address, address.Port);


                socket.ReceiveTimeout = 10 * 1000;
                socket.SendTimeout = 3 * 1000;
                socket.ReceiveBufferSize = this.bufferSize;
                socket.SendBufferSize = this.bufferSize;

                if (socket.Connected)
                {
                    using (NetworkStream netStream = new NetworkStream(socket))
                    {
                        if (netStream.CanWrite)
                        {
                            netStream.Write(headersData, 0, headersData.Length);

                            netStream.Flush();
                        }


                        if (netStream.CanRead)
                        {
                            response = new Response();

                            using (BinaryReader reader = new BinaryReader(netStream))
                            {


                                //解析出头数据
                                using (MemoryStream headerStream = new MemoryStream())
                                {

                                    byte[] stopSign = Encoding.ASCII.GetBytes("\r\n\r\n");

                                    int stopSignSize = stopSign.Length;
                                    int signSize = stopSignSize * 2;

                                    byte[] sign = new byte[signSize];


                                    int n = 0;
                                    while (!HasSign(sign, stopSign))
                                    {
                                        byte b = reader.ReadByte();

                                        for (int i = 0; i < signSize - 1; i++)
                                        {
                                            sign[i] = sign[i + 1];
                                        }

                                        sign[signSize - 1] = b;

                                        n++;

                                        if (n > stopSignSize)
                                        {
                                            headerStream.WriteByte(sign[signSize - 1]);
                                        }
                                    }

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
                                    }
                                }


                                //解析Body 数据

                                if (response.Headers["Content-Length"] != null)
                                {
                                    int contentLength = int.Parse(response.Headers["Content-Length"]);


                                    response.BodyStream = new MemoryStream(contentLength);

                                    byte[] buffer = new byte[this.bufferSize];
                                    int readCount;

                                    while ((readCount = netStream.Read(buffer, 0, this.bufferSize)) != 0)
                                    {
                                        response.BodyStream.Write(buffer, 0, readCount);
                                    }

                                    response.BodyStream.Seek(0, SeekOrigin.Begin);
                                }
                                else if (response.Headers["Transfer-Encoding"] == "chunked")
                                {
                                    response.BodyStream = new MemoryStream();

                                    byte[] stopSign = Encoding.ASCII.GetBytes("\r\n");

                                    int stopSignSize = stopSign.Length;
                                    int signSize = stopSignSize * 2;

                                    int chunkedLenth = 0;
                                    while (true)
                                    {
                                        byte[] sign = new byte[signSize];
                                        List<byte> bytes = new List<byte>();

                                        int n = 0;
                                        while (!HasSign(sign, stopSign))
                                        {
                                            byte b = reader.ReadByte();

                                            for (int i = 0; i < signSize - 1; i++)
                                            {
                                                sign[i] = sign[i + 1];
                                            }

                                            n++;

                                            if (n > stopSignSize)
                                            {
                                                bytes.Add(sign[signSize - 1]);
                                            }
                                        }

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
                                            while (totalCount < chunkedLenth)
                                            {
                                                int readCount = netStream.Read(buffer, 0, bufferSize);

                                                if (readCount != 0)
                                                {
                                                    totalCount += readCount;
                                                    response.BodyStream.Write(buffer, 0, readCount);
                                                }
                                                else
                                                {
                                                    break;
                                                }
                                            }

                                        }

                                        response.BodyStream.Seek(0, SeekOrigin.Begin);

                                    }
                                }
                                else
                                {
                                    response.BodyStream = new MemoryStream();
                                    byte[] buffer = new byte[this.bufferSize];
                                    int readCount;
                                    while ((readCount = netStream.Read(buffer, 0, this.bufferSize)) != 0)
                                    {
                                        response.BodyStream.Write(buffer, 0, readCount);
                                    }
                                    response.BodyStream.Seek(0, SeekOrigin.Begin);
                                }


                                reader.Close();
                                reader.Dispose();
                            }

                        }


                        netStream.Close();
                        netStream.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                if (socket != null)
                {
                    socket.Disconnect(true);
                }
            }
          
            return response;
        }


    }
}
