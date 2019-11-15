using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Timers;
using System.Threading;

namespace CardDispenser
{
    unsafe static class Program
    {
        private static bool Checks(int _rc, byte _ReType)
        {
            bool checksResult;
            checksResult = false;
            if (_rc == 0)
            {
                if (_ReType == 0x50)
                {
                    Console.WriteLine("Positive response");
                    checksResult = true;

                }
                if (_ReType == 0x4e)
                {
                    Console.WriteLine("Negative response");
                    // Command sending failed or command execution failed
                    //Console.WriteLine(ResErrMsg(St1, St2));
                }
            }
            else
            {
                Console.WriteLine("Communication error");
            }
            return checksResult;
        }

        //[DllImport("CRT_571.dll", CharSet = CharSet.Ansi)]
        //public static extern IntPtr SensorStatus();
        //[DllImport("CRT_571.dll", CharSet = CharSet.Ansi)]
        //public static extern IntPtr ResErrMsg(byte st1, byte st2);
        [DllImport("CRT_571.dll", CharSet = CharSet.Ansi)]
        private static extern int CommClose(IntPtr ComHandle);
        [DllImport("CRT_571.dll", CharSet = CharSet.Ansi)]
        private static extern IntPtr CommOpen(String Port);
        [DllImport("CRT_571.dll", CharSet = CharSet.Ansi)]
        private static extern IntPtr CommOpenWithBaut(String Port, uint Baudrate = 9600);
        [DllImport("CRT_571.dll", CharSet = CharSet.Ansi)]
        private static extern IntPtr CommSetting(IntPtr ComHandle, String ComSeting);
        [DllImport("CRT_571.dll", CharSet = CharSet.Ansi)]
        private static extern int ExecuteCommand(IntPtr ComHandle,
                                                    byte TxAddr,
                                                    byte TxCmCode,
                                                    byte TxPmCode,
                                                    int TxDataLen,
                                                    byte[] TxData,
                                                    ref byte RxReplyType,
                                                    ref byte RxStCode0,
                                                    ref byte RxStCode1,
                                                    ref byte RxStCode2,
                                                    ref int RxDataLen,
                                                    byte[] RxData
            );
        [DllImport("CRT_571.dll")]
        unsafe private static extern int ICCardTransmit(IntPtr ComHandle, byte TxAddr, byte TxCmCode, byte TxPmCode, int TxDataLen, byte[] TxData, ref byte[] RxReplyType, ref byte[] RxCmCode, ref byte[] RxPmCode, ref byte[] RxStCode0, ref byte[] RxStCode1, ref byte[] RxStCode2, ref int RxDataLen, byte[] RxData);
        static void Main()
        {
            byte[] CmData = new byte[1024];
            byte Addr;
            byte CmCode;
            byte PmCode;
            int CmDataLen;
            byte ReType = 0xFE;
            byte St2 = 0xFE;
            byte St1 = 0xFE;
            byte St0 = 0xFE; 
            int ReDataLen = 0;
            byte[] ReData = new byte[1024];
            int rc=0;
            System.Timers.Timer timer;
            try
            {
                //string port = File.ReadAllLines("ports.txt")[2];
                IntPtr hCom = CommOpenWithBaut("COM9", 9600);
                Console.WriteLine($"COMID: {hCom}");
                CmData = Enumerable.Repeat((byte)0x00, CmData.Length).ToArray();
                Addr = 0x00;
                CmCode = 0x32; // move card
                //PmCode = 0x39; // Parameter code move card without hold
                PmCode = 0x30; // Parameter code move card hold
                //CmCode = 0x31; // get stateus
                //PmCode = 0x31; // Parameter code sensor status
                CmDataLen = 0; // Data size (bytes) 
                //CmData[0] = 0x30; 
                rc = ExecuteCommand(hCom, Addr, CmCode, PmCode, CmDataLen, CmData,
                    ref ReType, ref St0, ref St1, ref St2, ref ReDataLen, ReData);
                Console.WriteLine($"rc:{rc}");
                Console.WriteLine("Retype1: {0:X}", ReType);
                Console.WriteLine($"St0: {0:St0}, St1: {0:St1}, St2: {1:St2}");
                if (Checks(rc, ReType))
                {
                    System.Threading.Thread.Sleep(1500);
                    CmCode = 0x31; // get stateus
                    PmCode = 0x31; // Parameter code sensor status
                    rc = ExecuteCommand(hCom, Addr, CmCode, PmCode, CmDataLen, CmData,
                        ref ReType, ref St0, ref St1, ref St2, ref ReDataLen, ReData);
                    Console.WriteLine($"rc:{rc}");
                    Console.WriteLine($"Retype1: {0:ReType}");
                    Console.WriteLine($"St0: {0:St0}, St1: {0:St1}, St2: {1:St2}");
                    if (Checks(rc, ReType))
                    {
                        for (int i = 0; i < ReDataLen; i++)
                        {
                            Console.WriteLine($"i{i}:{ReData[i]}");
                        }
                        if (ReData[0] == 49)
                        {
                            timer = new System.Timers.Timer(10000);
                            timer.Start();
                            timer.Elapsed += (s, e) =>
                            {
                                CmCode = 0x32; // move card
                                PmCode = 0x32; // Parameter code move card to RF
                                hCom = CommOpenWithBaut("COM5", 9600);
                                rc = ExecuteCommand(hCom, Addr, CmCode, PmCode, CmDataLen, CmData,
                                ref ReType, ref St0, ref St1, ref St2, ref ReDataLen, ReData);
                                Console.WriteLine($"rc:{rc}");
                                Console.WriteLine($"Retype1: {0:ReType}");
                                Console.WriteLine($"St0: {0:St0}, St1: {0:St1}, St2: {1:St2}");
                                Checks(rc, ReType);
                                timer.Stop();
                            };
                            timer.Dispose();
                        }
                    }
                }
                var closeResult=CommClose(hCom);
                Console.WriteLine($"CommClose result:{closeResult}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.ReadKey();
        }
    }
}
