using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace HttpTest
{
    class Program
    {
        static void Main(string[] args)
        {



            Uri uri = new Uri("http://www.baidu.com/");

            StringBuilder requestStringBuilder = new StringBuilder();


            requestStringBuilder.AppendFormat("GET {0} {1}/1.1\r\n", uri.PathAndQuery, uri.Scheme.ToUpper());

            requestStringBuilder.AppendFormat("Host: {0}\r\n", uri.Host);

            requestStringBuilder.AppendFormat("Connection: Close\r\n");


            requestStringBuilder.AppendFormat("Cache-Control: {0}\r\n", "no-cache");
            requestStringBuilder.AppendFormat("User-Agent: {0}\r\n", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/35.0.1916.153 Safari/537.36");
            requestStringBuilder.AppendFormat("Accept: {0}\r\n", uri.Host);
            requestStringBuilder.AppendFormat("Accept-Encoding: {0}\r\n", "gzip,deflate,sdch");
            requestStringBuilder.AppendFormat("Accept-Language: {0}\r\n", "zh-CN,zh;q=0.8");


            requestStringBuilder.Append("\r\n");




            byte[] data = Encoding.ASCII.GetBytes(requestStringBuilder.ToString());



            /*
             
             */




            HttpClient client = new HttpClient();
            client.BufferSize = 16;






            IPEndPoint point = new IPEndPoint(IPAddress.Parse("180.97.33.107"), 80);
           // /*
            for (int i = 0; i < 1000; i++)
            {

                var s1 = Stopwatch.StartNew();
                var httpData = client.GetHttpData(point, data);
                Console.WriteLine(s1.ElapsedMilliseconds);
               



                Thread.Sleep(1500);
            }
            //*/

            HttpHelper hh = new HttpHelper();

            for (int i = 0; i < 1000; i++)
            {  

            var s2 = Stopwatch.StartNew();
            var httpdata = hh.GetHttpData(point, data);
            Console.WriteLine(s2.ElapsedMilliseconds);
                
            }


            /*
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
             
            */





            //Console.WriteLine("hello");
            //Console.Read();




        }
    }
}
