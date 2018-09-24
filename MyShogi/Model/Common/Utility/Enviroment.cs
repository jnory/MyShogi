﻿using System;
using System.Management;

namespace MyShogi.Model.Common.Utility
{
    /// <summary>
    /// 物理空きメモリなどを調べるクラス
    /// </summary>
    public static class Enviroment
    {
        /// <summary>
        /// 現在使用されていない利用可能な物理メモリのサイズ(kB)
        /// </summary>
        /// <returns></returns>
        public static ulong GetFreePhysicalMemory()
        {
            ulong result = 0;
            try
            {
                using (var mc = new ManagementClass("Win32_OperatingSystem"))
                using (var moc = mc.GetInstances())
                    foreach (var mo in moc) // 何故か複数あることが想定されている。NUMA環境か？
                    {
                        result = (ulong)mo["FreePhysicalMemory"]; // このメンバ存在しないということはないはず。

                        mo.Dispose(); // これ要るのか？
                    }
            }
            catch { }

            if (!Environment.Is64BitOperatingSystem)
            {
                // 32bitで動作しているプロセス空間では、2GBまでしか物理メモリを扱えないので
                // 物理メモリがいかにあろうと2GBであるとみなす。
                result = System.Math.Min(result, 2 * 1024 * 1024ul);
            }

            return result;
        }

        /// <summary>
        /// 実行環境のスレッドの数
        /// HTオンだと、論理スレッド数、
        /// HTオフだと、物理スレッド数が返る。
        /// 
        /// AutoSettingでThreadを指定してある場合、現環境のスレッド数に制限されるべきなので
        /// そのためにこのメソッドが必要。
        /// </summary>
        /// <returns></returns>
        public static int GetProcessorCount()
        {
            // 10[us]以下で実行できるのでcacheしない。
            return Environment.ProcessorCount;
        }

        /// <summary>
        /// 実行環境の物理コア数。
        /// 
        /// AutoSettingでThreadを指定してある場合、現環境のスレッド数に制限されるべきなので
        /// そのためにこのメソッドが必要。
        /// </summary>
        /// <returns></returns>
        public static int GetProcessorCores()
        {
            var info = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "sysctl",
                // WorkingDirectory = engineData.ExeWorkingDirectory,
                Arguments = "hw.activecpu",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = false,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
            };

            var process = new System.Diagnostics.Process
            {
                StartInfo = info,
            };

            process.Start();

            string result = process.StandardOutput.ReadToEnd();
            result = result.Trim();
            result = result.Substring(14);

            bool success = Int32.TryParse(result, out processor_cores);
            if(!success) {
                processor_cores = 1;
            }

            return processor_cores;
        }

        /// <summary>
        /// 一度調べた物理コア数をcacheしておくための変数。
        /// </summary>
        private static int processor_cores;


    }
}
