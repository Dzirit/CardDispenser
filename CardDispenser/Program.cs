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
    unsafe class Program
    {
        [DllImport("CRT_571.dll", CharSet = CharSet.Ansi)]
        public static extern IntPtr CommOpen(String Port);

        [DllImport("CRT_571.dll", CharSet = CharSet.Ansi)]
        public static extern IntPtr CommSetting(IntPtr ComHandle, String ComSeting);

        [DllImport("CRT_571.dll", CharSet = CharSet.Ansi)]
        public static extern IntPtr CommOpenWithBaut(String Port, uint Baudrate = 9600);
        //[DllImport("CRT_571.dll", CharSet = CharSet.Ansi)]
        //public static extern IntPtr SensorStatus();
        //[DllImport("CRT_571.dll", CharSet = CharSet.Ansi)]
        //public static extern IntPtr ResErrMsg(byte st1, byte st2);

        [DllImport("CRT_571.dll", CharSet = CharSet.Ansi)]
        public static extern int CommClose(IntPtr ComHandle);
       

        [DllImport("CRT_571.dll", CharSet = CharSet.Ansi)]
        public static extern int ExecuteCommand(IntPtr ComHandle,
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
        public static extern int ICCardTransmit(IntPtr ComHandle, byte TxAddr, byte TxCmCode, byte TxPmCode, int TxDataLen, byte[] TxData, ref byte[] RxReplyType, ref byte[] RxCmCode, ref byte[] RxPmCode, ref byte[] RxStCode0, ref byte[] RxStCode1, ref byte[] RxStCode2, ref int RxDataLen, byte[] RxData);

        public void ExecuteAndCheck(IntPtr _hCom, byte _CmCode, byte _PmCode, byte[] _ReData, int _ReDataLen)
        {
            byte[] CmData = new byte[1024];
            byte Addr = 0x00;
            int CmDataLen=0;
            byte ReType = 0xFE;
            byte St2 = 0xFE;
            byte St1 = 0xFE;
            byte St0 = 0xFE;
            int rc = 0;
            rc = ExecuteCommand(_hCom, Addr, _CmCode, _PmCode, CmDataLen, CmData,
                    ref ReType, ref St0, ref St1, ref St2, ref _ReDataLen, _ReData);
            Console.WriteLine($"rc:{rc}");
            Console.WriteLine("Retype1: {0:X}", ReType);
            Console.WriteLine("St0: {0:X}, St1: {0:X}, St2: {1:X}", St0, St1, St2);
        }
        static void Main(string[] args)
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
                IntPtr hCom = CommOpenWithBaut("COM5", 9600);
                Console.WriteLine(String.Format("COMID: {0}", hCom));

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
                Console.WriteLine("St0: {0:X}, St1: {0:X}, St2: {1:X}", St0, St1, St2);
                if ((int)rc == 0)
                {
                    if (ReType == 0x50)
                    {
                        Console.WriteLine("Positive response");
                        System.Threading.Thread.Sleep(1500);
                        CmCode = 0x31; // get stateus
                        PmCode = 0x31; // Parameter code sensor status
                        rc = ExecuteCommand(hCom, Addr, CmCode, PmCode, CmDataLen, CmData,
                    ref ReType, ref St0, ref St1, ref St2, ref ReDataLen, ReData);
                        Console.WriteLine($"rc:{rc}");
                        Console.WriteLine("Retype1: {0:X}", ReType);
                        Console.WriteLine("St0: {0:X}, St1: {0:X}, St2: {1:X}", St0, St1, St2);
                        if ((int)rc == 0)
                        {
                            if (ReType == 0x50)
                            {
                                Console.WriteLine("Positive response");
                                for (int i = 0; i < ReDataLen; i++)
                                {
                                    Console.WriteLine($"i{i}:{ReData[i]}");
                                }
                                if (ReData[0] == 49)
                                {
                                    timer = new System.Timers.Timer(10000);
                                    timer.Start();
                                    timer.Elapsed += (s, e) => {
                                        CmCode = 0x32; // move card
                                        PmCode = 0x32; // Parameter code move card to RF
                                        hCom = CommOpenWithBaut("COM5", 9600);
                                        rc = ExecuteCommand(hCom, Addr, CmCode, PmCode, CmDataLen, CmData,
                                        ref ReType, ref St0, ref St1, ref St2, ref ReDataLen, ReData);
                                        Console.WriteLine($"rc:{rc}");
                                        Console.WriteLine("Retype1: {0:X}", ReType);
                                        Console.WriteLine("St0: {0:X}, St1: {0:X}, St2: {1:X}", St0, St1, St2);
                                        if ((int)rc == 0)
                                        {
                                            if (ReType == 0x50)
                                            {
                                                Console.WriteLine("Positive response");

                                            }
                                            if (ReType == 0x4e)
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
                                        timer.Stop();
                                    };
                                }
                                
                            }
                            if (ReType == 0x4e)
                            {
                                Console.WriteLine("Negative response");
                                // Command sending failed or command execution failed
                                // Console.WriteLine(ResErrMsg(St1, St2));
                            }
                        }
                        else
                        {
                            Console.WriteLine("Communication error");
                        }
                    }
                    if (ReType == 0x4e)
                    {
                        Console.WriteLine("Negative response");
                        // Command sending failed or command execution failed
                        // Console.WriteLine(ResErrMsg(St1, St2));
                    }
                }
                else
                {
                    Console.WriteLine("Communication error");
                }
                
                
                CommClose(hCom);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.ReadKey();
        }
       
    }
}
