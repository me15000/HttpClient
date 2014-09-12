using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace HttpTest
{
    class Program
    {
        static void Main(string[] args)
        {



            Uri uri = new Uri("http://www.baidu.com/");

            StringBuilder requestStringBuilder = new StringBuilder();


            requestStringBuilder.AppendFormat("GET {0} {1}/1.1\r\n", uri.ToString(), uri.Scheme.ToUpper());

            requestStringBuilder.AppendFormat("Host: {0}\r\n", uri.Host);

            requestStringBuilder.AppendFormat("Connection: Close\r\n");



            requestStringBuilder.Append("\r\n");




            byte[] data = Encoding.ASCII.GetBytes(requestStringBuilder.ToString());








            HttpClient client = new HttpClient();

            IPEndPoint point = new IPEndPoint(IPAddress.Parse("180.97.33.108"), 80);

            var httpData = client.GetHttpData(point, data);

            if (httpData != null)
            {
                foreach (string key in httpData.Headers.AllKeys)
                {
                    Console.Write(key);
                    Console.Write(":");
                    Console.WriteLine(httpData.Headers[key]);
                }
            }


            using (FileStream fs = File.Create("./index.html"))
            {
                using (var stream = httpData.BodyStream)
                {
                    byte[] buffer = new byte[256];
                    int readCount;
                    while ((readCount = stream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        fs.Write(buffer, 0, readCount);
                        fs.Flush();
                    }
                }
                

                fs.Close();
                fs.Dispose();
            }

            Console.WriteLine("hello");
            Console.Read();




        }
    }
}
