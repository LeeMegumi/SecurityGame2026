/*!
 * \file
 * \brief Get distance data from Ethernet type URG
 * \author Jun Fujimoto
 * $Id: get_distance_ethernet.cs 403 2013-07-11 05:24:12Z fujimoto $
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using SCIP_library;

class get_distance_ethernet
{
    static void Main(string[] args)
    {
        const int GET_NUM = 10;
        const int start_step = 0;
        const int end_step = 760;
        try {
            string ip_address;
            int port_number;
            get_connect_information(out ip_address, out port_number);

            TcpClient urg = new TcpClient();
            urg.Connect(ip_address, port_number);
            NetworkStream stream = urg.GetStream();

            write(stream, SCIP_Writer.SCIP2());
            read_line(stream); // ignore echo back
            write(stream, SCIP_Writer.MD(start_step, end_step));
            read_line(stream);  // ignore echo back

            List<long> distances = new List<long>();
            long time_stamp = 0;
            for (int i = 0; i < GET_NUM; ++i) {
                string receive_data = read_line(stream);
                if (!SCIP_Reader.MD(receive_data, ref time_stamp, ref distances)) {
                    Console.WriteLine(receive_data);
                    break;
                }
                if (distances.Count == 0) {
                    Console.WriteLine(receive_data);
                    continue;
                }
                // show distance data
                Console.WriteLine("time stamp: " + time_stamp.ToString() + " distance[100] : " + distances[100].ToString());
            }
            write(stream, SCIP_Writer.QT());    // stop measurement mode
            read_line(stream); // ignore echo back
            stream.Close();
            urg.Close();
        } catch (Exception ex) {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        } finally {
            Console.WriteLine("Press any key.");
            Console.ReadKey();
        }
    }

    /// <summary>
    /// get connection information from user.
    /// </summary>
    private static void get_connect_information(out string ip_address, out int port_number)
    {
        ip_address = "192.168.0.10";
        port_number = 10940;
        Console.WriteLine("Please enter IP Address. [default: " + ip_address + "]");
        string str = Console.ReadLine();
        if (str != "") {
            ip_address = str;
        }
        Console.WriteLine("Please enter Port number. [default: " + port_number.ToString() + "]");
        str = Console.ReadLine();
        if (str != "") {
            port_number = int.Parse(str);
        }

        Console.WriteLine("Connect setting = IP Address : " + ip_address + " Port number : " + port_number.ToString());
    }

    /// <summary>
    /// Read to "\n\n" from NetworkStream
    /// </summary>
    /// <returns>receive data</returns>
    static string read_line(NetworkStream stream)
    {
        if (stream.CanRead) {
            StringBuilder sb = new StringBuilder();
            bool is_NL2 = false;
            bool is_NL = false;
            do {
                char buf = (char)stream.ReadByte();
                if (buf == '\n') {
                    if (is_NL) {
                        is_NL2 = true;
                    } else {
                        is_NL = true;
                    }
                } else {
                    is_NL = false;
                }
                sb.Append(buf);
            } while (!is_NL2);

            return sb.ToString();
        } else {
            return null;
        }
    }

    /// <summary>
    /// write data
    /// </summary>
    static bool write(NetworkStream stream, string data)
    {
        if (stream.CanWrite) {
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            stream.Write(buffer, 0, buffer.Length);
            return true;
        } else {
            return false;
        }
    }
}