using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Console;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SwitchGenshin
{
    internal class Program
    {
        /// <summary>
        /// 参数容器ParamContainer（字典）中存放的值
        /// </summary>
        private struct Value
        {
            public Value(string Str, long Index)
            {
                value = Str;
                index = Index;
            }
            public string value;
            public long index;
        }

        /// <summary>
        /// 参数容器
        /// </summary>
        class ParamContainer : Dictionary<string, Value>
        {
            /// <summary>
            /// 不允许无参数初始化
            /// </summary>
            private ParamContainer() { }

            /// <summary>
            /// 初始化参数容器
            /// </summary>
            /// <param name="name">该容器的标签，也就是名称</param>
            /// <param name="index">该名称在文件中的索引位置</param>
            public ParamContainer(string value, long index = 0)
            {
                Value = value;
                Index = index;
            }

            /// <summary>
            /// 此参数集合的名称
            /// </summary>
            public string Value { get; }
            /// <summary>
            /// 此参数集合在文件中的位置
            /// </summary>
            public long Index { get; }
        }

        /// <summary>
        /// 本软件所在的启动路径
        /// </summary>
        private static readonly string LocalPath = Application.StartupPath;

        /// <summary>
        /// Windows通用换行符
        /// </summary>
        private const string CRLF = "\r\n";

        /// <summary>
        /// 配置文件路径（承接程序启动路径）
        /// </summary>
        private const string MyconfigName = @"config.ini";

        /// <summary>
        /// 资源文件夹路径（承接程序启动路径）
        /// </summary>
        private const string ResourcesDir = @"Resources";

        /// <summary>
        /// 备份文件夹路径（承接程序启动路径）
        /// </summary>
        private const string BackupAddr = @"Resources\Backup";

        /// <summary>
        /// 动态链接库文件名
        /// </summary>
        private const string DllName = "PCGameSDK.dll";

        /// <summary>
        /// 初始化配置文件名
        /// </summary>
        private const string ConfigName = "config.ini";

        /// <summary>
        /// 软件文件夹名
        /// </summary>
        private const string SoftwareDirName = "Genshin Impact";

        /// <summary>
        /// 文件PCGameSDK.dll所在文件夹（承接软件路径）
        /// </summary>
        private const string DllDir = @"Genshin Impact Game\YuanShen_Data\Plugins";

        /// <summary>
        /// 文件config.ini所在文件夹（承接软件路径）
        /// </summary>
        private const string ConfigDir = @"Genshin Impact Game";

        // 预定义标签
        private const string KEY_PATH = "Path";         // 路径信息标签
        private const string VALUE_SOFT_DIR = "SoftwareDir";

        private const string AIM_KEY = "General";       // 执行切换时的关键字标签（参数组名）
        private const string AIM_VALUE_CHANNEL = "channel";
        private const string AIM_VALUE_CPS = "cps";

        static void Main(string[] args)
        {
            /// 暂时想到的是全部写入的方法。
            /// 如果是大文件，这种将文件全部内容读到内存里的做法不可取，需要研究一下。
            /// 尤其是文件中间插入文字的办法。

            #region 提取图标的代码
            //string savePathbmp = @"C:\Users\ppyder\OneDrive\桌面\Picture.bmp";

            //string savePathico = @"C:\Users\ppyder\OneDrive\桌面\Picture.ico";

            //string tempPath = @"D:\ProgramFiles_Mine\Genshin Impact\launcher.exe";

            //Icon GenshinIcon = MyFileInfo.GetSmallIcon(tempPath);

            //FileStream Pic_fs = File.Create(savePathico);

            //GenshinIcon.Save(Pic_fs);
            //Pic_fs.Flush();
            //Pic_fs.Close();

            //GenshinIcon.ToBitmap().Save(savePathbmp);
            #endregion

            //设置窗口标题
            Console.Title = "Genshin登录服务器切换  #=== Powered by ppyder ++ Mail: 942041771@qq.com ===#";

            // 基本信息显示
            WriteLine("\n++ Genshin登录服务器切换程序 ++\n\n#=== Powered by ppyder ++ Mail: 942041771@qq.com ===#\n");

            WriteLine($"\n---\n当前软件的工作路径（进程所在路径）为：{Environment.CurrentDirectory}" +
                      $"\n当前软件的启动路径（软件本体所在路径）为：{Application.StartupPath}\n");

            // 将工作路径重新设置为软件本体所在路径，方便操作，也更安全。
            Environment.CurrentDirectory = Application.StartupPath;

            WriteLine($"\n---\n已将工作路径重定向为：{Environment.CurrentDirectory}\n");

            if (Directory.Exists(Path.Combine(LocalPath, ResourcesDir)))
            { WriteLine("\n---\n检测到资源目录已存在。\n"); }
            else
            { WriteLine("\n---\n检测到资源目录不存在，即将创建。\n"); }

            // 创建资源目录（如果目录存在则不创建，返回已存在的目录信息）
            DirectoryInfo ResDirInfo = Directory.CreateDirectory(Path.Combine(LocalPath, ResourcesDir));
            WriteLine($"资源目录的绝对路径为：{ResDirInfo.FullName}\n");

            // 创建初始化文件，如果存在则不创建
            if (!File.Exists(Path.Combine(LocalPath, MyconfigName)))
            {
                // 创建完成后利用其返回的实例将文件关闭，释放资源
                File.Create(Path.Combine(LocalPath, MyconfigName)).Close();
            }

            // 读取本地配置信息
            Dictionary<string, ParamContainer> MyParams;
            try
            {
                // 从初始化文件中读取配置参数
                MyParams = ReadConfigFile(Path.Combine(LocalPath, MyconfigName));
            }
            catch (Exception ex)
            {
                Write("\n" + ex.ToString());
                MessageBox.Show("出错了！请留意控制台输出的错误信息。", "Opps!");
                return;
            }

            // 执行流程分支：如果有参数输入，则执行配置更新；如果没有，则进行正常的切换操作。
            if (args.Length > 0)
            {
                foreach (string arg in args)
                {
                    WriteLine("已获取到参数：" + arg);
                    //ReadKey();
                    if (arg.EndsWith(DllName))
                    {
                        // 以获取到PCGameSDK.dll路径的思路去解析信息，如果解析失败，则直接结束程序
                        if (!GetDllFullPath(arg, ref MyParams, false))
                        { MessageBox.Show("请留意控制台输出的提示信息。", "Opps!"); return; }
                    }
                    else if (arg.EndsWith(SoftwareDirName))
                    {
                        // 直接把文件目录加上按照上述思路解析信息
                        if (!GetDllFullPath(Path.Combine(arg, DllDir, DllName), ref MyParams, false))
                        { MessageBox.Show("请留意控制台输出的提示信息。", "Opps!"); return; }
                    }
                    else { } // 其他输入不做处理
                }
                // 数据更新完成后结束程序运行
                return;
            }
            else { } // 什么也不做

            // 目标文件的参数容器
            Dictionary<string, ParamContainer> SoftwareParams = null;

            // 看看是否需要对软件参数进行初始化
            if (0 == MyParams.Count)
            {
                WriteLine("\n---\n检测到本地配置不存在，尝试在注册表中查找软件安装信息...\n");
                if (GetSoftwarePathFromReg("原神", SoftwareDirName, out string test))
                {
                    WriteLine("\n已在注册表中查找到软件安装路径：" + test + "\n");

                    // 直接把文件目录加上，按照前述思路解析信息
                    if (!GetDllFullPath(Path.Combine(test, DllDir, DllName), ref MyParams, true))
                    { MessageBox.Show("请留意控制台输出的提示信息。", "Opps!"); return; }
                }
                else // 需要手动初始化软件
                {
                    WriteLine("\n---\n查找失败。需要生成配置文件，请遵照提示执行操作。\n");

                    // 原本配置文件不存在的情况，需要提示用户初始化一次软件状态
                    MessageBox.Show("未在本地找到软件安装路径信息，请首先确认是否已经安装了原神。\n" +
                                    "\n++=====++\n" +
                                    "执行手动初始化操作：\n" +
                                    "1. 找到软件所在文件夹；\n" +
                                    "2. 将文件夹图标拖动到本软件图标上（使用本软件打开文件夹）；\n" +
                                    "3. 如果出现了预料之外的报错或软件闪退，可联系软件作者对其进行排错和升级。:)",
                                    "软件需要进行手动初始化");

                    WriteLine("\n程序正常结束。");
                    return;
                }
            }
            else { } // Do Notihing.

            // 检查参数有效性。如果一切正常，SoftwareParams将在其中被初始化。
            if (!CheckParams_Files(ref MyParams, ref SoftwareParams))
            { MessageBox.Show("请留意控制台输出的错误信息。", "Opps!"); return; }

            // 获取本地保存的软件路径
            string SoftwareDir = MyParams[KEY_PATH][VALUE_SOFT_DIR].value;

            // 查询软件当前状态(1.PCGameSDK.dll存在，2.config.ini中的[General]channel = 14，cps = bilibili即为bilibil登录)
            bool isBilibili = File.Exists(Path.Combine(SoftwareDir, DllDir, DllName)) &&
                        (SoftwareParams[AIM_KEY][AIM_VALUE_CHANNEL].value == "14" && SoftwareParams[AIM_KEY][AIM_VALUE_CPS].value == "bilibili");

            // 询问是否切换
            bool isToSwitch = MessageBox.Show("目前的服务器选项是 [" + (isBilibili ? "Bilibili" : "Mihoyo") + "]，确定要切换吗？",
                                         "确认操作", MessageBoxButtons.YesNo) == DialogResult.Yes;

            try // 尝试执行切换任务
            {
                if (isToSwitch)
                {
                    // 执行切换
                    if (isBilibili ? SwitchToMiHoYo(SoftwareDir, ref SoftwareParams) : SwitchToBiliBili(SoftwareDir, ref SoftwareParams))
                    {
                        isBilibili = !isBilibili;
                    }
                    else
                    {
                        WriteLine("\n---\n切换时出错，请联系作者排错。");
                    }
                    //根据标志给出提示
                    MessageBox.Show($"Done！已切换为 [" + (isBilibili ? "Bilibili" : "Mihoyo") + "] 服务器登录！\n" +
                                    "\n++=====++\n" +
                                    "其他提示（如果切换后游戏运行不正常）：\n" +
                                    "1. 可通过本软件目录下的Resource\\Backup文件夹中的文件进行回退， 注意阅读其中的回退指导！\n" +
                                    "2. 如果希望继续使用本软件进行快捷切换，可联系软件作者对其进行升级。:)", "玩得开心！");
                }
            }
            catch (Exception ex)
            {
                Write("\n" + ex.ToString());
                MessageBox.Show("出错了！请留意控制台输出的错误信息。", "Opps!");
                return;
            }

            WriteLine("\n程序正常结束。");

            return;
        }

        /// <summary>
        /// 对自身配置文件以及本地备份文件进行检查校验
        /// </summary>
        /// <param name="myParams">从配置文件中读取出来的参数</param>
        /// <returns>返回合法性检查的结果，参数有效返回true。</returns>
        static bool CheckParams_Files(ref Dictionary<string, ParamContainer> myParams,
                                      ref Dictionary<string, ParamContainer> SoftParams)
        {
            // 从参数组合中获取软件安装路径
            string SoftwarePath = myParams[KEY_PATH][VALUE_SOFT_DIR].value;

            // 检测软件路径有效性
            if (Directory.Exists(SoftwarePath))
            {
                WriteLine("\n---\n软件安装路径有效性验证完成。\n");
            }
            else
            {
                WriteLine("\n---\n检测到本地保存的软件安装路无效，尝试在注册表中查找软件安装信息...\n");
                if (GetSoftwarePathFromReg("原神", SoftwareDirName, out SoftwarePath))
                {
                    WriteLine("\n已在注册表中查找到软件安装路径：" + SoftwarePath + "\n");

                    // 直接把文件目录加上，按照前述思路解析信息
                    if (!GetDllFullPath(Path.Combine(SoftwarePath, DllDir, DllName), ref myParams, true))
                    { return false; }
                }
                else // 需要手动初始化软件
                {
                    WriteLine("\n查找失败。需要生成配置文件，请遵照提示执行操作。\n");

                    // 原本配置文件不存在的情况，需要提示用户初始化一次软件状态
                    WriteLine("\n--- 软件需要进行手动初始化\n\n未在本地找到软件安装路径信息，请首先确认是否已经安装了原神。\n" +
                              "\n++=====++\n" +
                              "执行手动初始化操作：\n" +
                              "1.找到软件所在文件夹；\n" +
                              "2.将文件夹图标拖动到本软件图标上（使用本软件打开文件夹）；\n" +
                              "3.如果出现了预料之外的报错或软件闪退，可联系软件作者对其进行排错和升级。:)\n");

                    return false;
                }
            }

            // 检验软件文件夹结构有效性
            if (Directory.Exists(Path.Combine(SoftwarePath, DllDir))
                && File.Exists(Path.Combine(SoftwarePath, ConfigDir, ConfigName)))
            {
                WriteLine("\n---\n软件目录结构有效性验证完成。\n");
            }
            else
            {
                WriteLine($"\n---\n[0]检测到目前版本的软件文件目录相较本软件开发时已发生改变，请联系软件开发者升级软件。\n");
                return false;
            }

            // 检验目标目录下Dll文件是否版本一致，并进行相应处理
            if (File.Exists(Path.Combine(SoftwarePath, DllDir, DllName)))
            {
                DealLocalDll(Path.Combine(SoftwarePath, DllDir, DllName));
            }

            // 从目标配置文件中读取信息
            SoftParams = ReadConfigFile(Path.Combine(SoftwarePath, ConfigDir, ConfigName));

            // 首先检查目标文件中是否存在本软件功能实现所需的信息
            // （注意顺序不能错，这里利用了逻辑运算时的截断特性：当左边的表达式足以决定表达式结果时，后续将不进行下一步的运算）
            if (!(SoftParams != null && SoftParams.Keys.Contains(AIM_KEY)
                && SoftParams[AIM_KEY].Keys.Contains(AIM_VALUE_CHANNEL) && SoftParams[AIM_KEY].Keys.Contains(AIM_VALUE_CPS)))
            {
                // 不存在本软件逻辑实现所需的参数，提示排错
                WriteLine("\n---\n检测到软件目录下的配置文件中不存在本软件工作所需的参数，可能有如下情形：" +
                          "1.如果上次成功运行本软件后软件并未更新过，则可能是配置文件损坏，可通过本软件目录下的Resource文件夹中的备份文件进行恢复，" +
                          "注意阅读其中的回退指导！\n" +
                          "2.目前版本下软件配置文件的工作逻辑相较本软件开发时已发生改变。若如此，请联系软件开发者升级软件。\n");
                return false;
            }
            else { } // 一切正常，什么也不做

            // 本地备份文件参数信息
            Dictionary<string, ParamContainer> BackParams = null;

            // 其次检查本地备份文件中是否存在本软件功能实现所需的信息
            if (File.Exists(Path.Combine(LocalPath, ResourcesDir, ConfigName)))
            {
                // 从本地备份文件中读取信息
                BackParams = ReadConfigFile(Path.Combine(LocalPath, ResourcesDir, ConfigName));

                // （注意顺序不能错，这里利用了逻辑运算时的截断特性：当左边的表达式足以决定表达式结果时，后续将不进行下一步的运算）
                if (!(BackParams != null && BackParams.Keys.Contains(AIM_KEY)
                    && BackParams[AIM_KEY].Keys.Contains(AIM_VALUE_CHANNEL) && BackParams[AIM_KEY].Keys.Contains(AIM_VALUE_CPS)))
                {
                    // 不存在本软件逻辑实现所需的参数，提示排错
                    WriteLine("\n---\n检测到本地配置文件中不存在本软件工作所需的参数，可能是此配置文件已损坏。将删除此文件。\n");

                    // 移除已损坏的备份文件，重新置空备份文件参数引用
                    File.Delete(Path.Combine(LocalPath, ResourcesDir, ConfigName));
                    BackParams = null;
                }
                else { } // 一切正常，什么也不做
            }
            else { } // 本地备份文件不存在，不做任何操作

            // 如果文件不存在，就直接拷贝一个变更前的副本到本地进行备份
            if (BackParams is null)
            {
                WriteLine("\n---\n检测到资源目录下的配置文件备份不存在，即将创建。\n");

                // 将新文件拷贝到本地留存以待后用
                File.Copy(Path.Combine(SoftwarePath, ConfigDir, ConfigName), Path.Combine(LocalPath, ResourcesDir, ConfigName));

                // 提示
                WriteLine("\n已将目标配置文件备份到本地。\n");
            }
            else // 若存在，则检查备份配置文件版本信息
            {
                try
                {
                    // tempParams用于存储已经检查过的键值对以便下次检查时跳过，用于提升程序性能
                    Dictionary<string, ParamContainer> ExceptParams = new Dictionary<string, ParamContainer>();

                    // 添加比对的排除项（因为这两项是要改变的）
                    ExceptParams.Add(AIM_KEY, new ParamContainer(AIM_KEY));
                    ExceptParams[AIM_KEY].Add(AIM_VALUE_CHANNEL, new Value());
                    ExceptParams[AIM_KEY].Add(AIM_VALUE_CPS, new Value());

                    WriteLine("\n\n---\n开始以「目标文件」为基准对「备份文件」进行参数匹配性检查...\n");

                    // 按照soft中有的参数，比对back中的参数
                    bool isEqual = CompareParamsEqual(SoftParams, BackParams, ref ExceptParams);

                    WriteLine("\n开始以「备份文件」为基准对「目标文件」进行参数匹配性检查（不会再显示已经检出的差异项）...\n");

                    // 由于可能存在back中有、soft中不存在的参数，所以还要换一下基准检查一次
                    isEqual &= CompareParamsEqual(BackParams, SoftParams, ref ExceptParams);

                    // 根据匹配结果决定处理方式
                    if (!isEqual)
                    {
                        WriteLine("\nDone.查出以上差异，备份配置文件版本一致性验证完成。\n");

                        // 文件不匹配，询问是否需要保存新的作为以后使用的文件
                        bool isChangeFile = DialogResult.Yes == MessageBox.Show("目标软件目录下的config文件与本地保存的文件不一致（可能版本更新或本地文件损坏），需要用它替换本地备份文件吗？",
                                                                                "提示", MessageBoxButtons.YesNo);
                        if (isChangeFile)
                        {
                            // 询问是否需要保留旧版本的文件
                            bool isSave = DialogResult.Yes == MessageBox.Show("是否保留旧文件（将作为备份保存在本软件根目录下）？",
                                                                              "提示", MessageBoxButtons.YesNo);
                            if (isSave)
                            {
                                // 获取文件后缀
                                string FileEnd = DateTime.Now.ToString().Replace("/", "-").Replace(" ", "_").Replace(":", "-") + ".save";

                                // 改名保存
                                File.Copy(Path.Combine(LocalPath, ResourcesDir, ConfigName), Path.Combine(LocalPath, ResourcesDir, ConfigName + "_Local_" + FileEnd));
                                File.Delete(Path.Combine(LocalPath, ResourcesDir, ConfigName));

                                WriteLine("\n已将备份配置文件改名备份到本地。\n");
                            }
                            else
                            {
                                // 直接删除
                                File.Delete(Path.Combine(LocalPath, ResourcesDir, ConfigName));

                                WriteLine("\n已将备份配置文件移除。\n");
                            }
                            // 将新文件拷贝到本地留存以待后用
                            File.Copy(Path.Combine(SoftwarePath, ConfigDir, ConfigName), Path.Combine(LocalPath, ResourcesDir, ConfigName));

                            // 提示
                            WriteLine("\n已将目标配置文件备份到本地。\n");
                        }
                        else
                        {
                            //继续使用本地保存的文件,并将软件参数替换为本地保存参数
                            SoftParams = BackParams;
                            WriteLine("\nConfig文件保持原状，本次软件运行也将按照本地备份文件指定的方式工作。\n");
                        }
                    }
                    else
                    {
                        WriteLine("\nDone.二者版本内容一致，备份配置文件版本一致性验证完成。\n");
                    }
                }
                catch (Exception ex)
                {
                    Write("\n" + ex.ToString());
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 比较两个参数组
        /// </summary>
        /// <param name="BaseParams">作为遍历基准进行比较检查的参数组</param>
        /// <param name="AimParams">被比较的参数组</param>
        /// <param name="ExceptParams">
        /// 引用参数，本次排查过程中需要排除的项（只会用到其中的key值）。本次运算结束后，将向其中追加本次检查过的项。
        /// </param>
        /// <returns>返回值为是否一致，一致为true</returns>
        static bool CompareParamsEqual(Dictionary<string, ParamContainer> BaseParams,
                                       Dictionary<string, ParamContainer> AimParams,
                                   ref Dictionary<string, ParamContainer> ExceptParams)
        {
            bool isEqual = true;

            //遍历对比目标文件中的参数信息
            foreach (KeyValuePair<string, ParamContainer> pair in BaseParams)
            {
                if (!AimParams.Keys.Contains(pair.Key))
                {
                    // 如果检测到备份文件中不存在目标文件中的关键字（参数组），则直接跳过对此参数组的对比检索
                    WriteLine($"相较于软件目录下的配置文件，检测到备份文件中不存在参数组 [{pair.Key}] \n");
                    isEqual = false;
                    continue;
                }
                else { } // 什么也不做，向下进行进一步的参数检查

                // 如果关键字不存在，则向排除队列中添加一个对应的参数容器
                if (!ExceptParams.Keys.Contains(pair.Key))
                { ExceptParams.Add(pair.Key, new ParamContainer(pair.Key)); }

                // 遍历对比参数信息
                foreach (KeyValuePair<string, Value> kv in pair.Value)
                {
                    // 跳过排除项
                    if (ExceptParams.Keys.Contains(pair.Key) && ExceptParams[pair.Key].Keys.Contains(kv.Key))
                    {
                        continue;
                    }
                    else if (!AimParams[pair.Key].Keys.Contains(kv.Key))
                    {
                        WriteLine($"检测到不同的配置：[{pair.Key}]中的参数中参数[{kv.Key}]在「前者」中存在，在「后者」中不存在。\n");
                        isEqual = false;
                    }
                    else if (kv.Value.value != AimParams[pair.Key][kv.Key].value)
                    {
                        WriteLine($"检测到不同的配置：[{pair.Key}]中参数[{kv.Key}]在「前者」中的值为：[{kv.Value.value}]，" +
                                  $"在「后者」中的值为：[{AimParams[pair.Key][kv.Key].value}]\n");
                        isEqual = false;

                        // 向排除项中添加共有项
                        ExceptParams[pair.Key].Add(kv.Key, new Value());
                    }
                    else { } // 两项匹配，什么也不做
                }
            }
            return isEqual;
        }

        /// <summary>
        /// 以MD5校验的方式检查两个文件的内容是否一致
        /// </summary>
        /// <param name="Path1">第一个文件的路径</param>
        /// <param name="Path2">第二个文件的路径</param>
        /// <returns>如果两个文件一致，则返回true。如果存在无效的文件路径，也会返回false</returns>
        static bool IsFileEqual(string Path1, string Path2)
        {
            if (!File.Exists(Path1) || !File.Exists(Path2))
            { return false; }

            if (Path1 == Path2)
            { return true; }

            // 以MD5校验工具校验文件
            MD5 md5 = new MD5CryptoServiceProvider();

            // 获取两个文件流
            FileStream Fs1 = File.OpenRead(Path1);
            FileStream Fs2 = File.OpenRead(Path2);

            // 计算MD5值(计算完成后的结果是一个byte数组)
            byte[] retVal_Local = md5.ComputeHash(Fs1);
            byte[] retVal_Software = md5.ComputeHash(Fs2);

            // 关闭流以释放资源
            Fs1.Close();
            Fs2.Close();

            // 逐个比对计算结果以确定两个目标文件是否一致
            bool isEqual = true;
            for (int i = 0; i < retVal_Software.Length; i++)
            {
                isEqual &= retVal_Software[i] == retVal_Local[i];
            }

            return isEqual;
        }

        /// <summary>
        /// 使用目标路径的DLL处理本地保存的DLL。调用此函数前应确保传入路径对应的的文件是存在的。
        /// </summary>
        /// <param name="DllPath">软件目录下的DLL路径</param>
        static void DealLocalDll(string DllPath)
        {
            // 尝试将目标文件放在软件目录下
            if (File.Exists(Path.Combine(LocalPath, ResourcesDir, DllName)))
            {
                // 以MD5校验工具校验文件是否一致
                if (!IsFileEqual(Path.Combine(LocalPath, ResourcesDir, DllName), DllPath))
                {
                    // 文件不匹配，询问是否需要保存新的作为以后使用的文件
                    bool isChangeFile = DialogResult.Yes == MessageBox.Show("目标软件目录下的dll文件与本地保存的dll文件不一致（可能版本更新或本地文件损坏），需要用它替换本地备份文件吗？",
                                                                            "提示", MessageBoxButtons.YesNo);
                    if (isChangeFile)
                    {
                        // 询问是否需要保留旧版本的文件
                        bool isSave = DialogResult.Yes == MessageBox.Show("是否保留旧文件（将作为备份保存在本软件根目录下）？",
                                                                          "提示", MessageBoxButtons.YesNo);
                        if (isSave)
                        {
                            // 获取文件后缀
                            string FileEnd = DateTime.Now.ToString().Replace("/", "-").Replace(" ", "_").Replace(":", "-") + ".save";

                            // 改名保存
                            File.Copy(Path.Combine(LocalPath, ResourcesDir, DllName), Path.Combine(LocalPath, ResourcesDir, DllName + "_Local_" + FileEnd));
                            File.Delete(Path.Combine(LocalPath, ResourcesDir, DllName));
                        }
                        else
                        {
                            // 直接删除
                            File.Delete(Path.Combine(LocalPath, ResourcesDir, DllName));
                        }
                        // 将新文件拷贝到本地留存以待后用
                        File.Copy(DllPath, Path.Combine(LocalPath, ResourcesDir, DllName));
                    }
                    else { } //什么也不做，继续使用本地保存的文件
                }
                else
                {
                    // 什么也不做
                    WriteLine("\n---\n校验完成：目标dll文件与本地保存的dll文件版本一致。");
                }
            }
            else
            {
                // 直接把目标文件拷贝到本地以待后用
                File.Copy(DllPath, Path.Combine(LocalPath, ResourcesDir, DllName));

                // 提示
                WriteLine("\n已将目标dll文件备份到本地。");
            }
            return;
        }

        /// <summary>
        /// 以输入参数获得文件PCGameSDK.dll时，对本地配置的更新操作
        /// </summary>
        /// <param name="DllPath">获得的路径信息</param>
        /// <param name="myParams">待更新的参数信息</param>
        /// /// <param name="isChangeConfigDirectly">是否越过询问直接变更本地配置</param>
        /// <returns>返回操作是否成功，如果成功，则返回true。</returns>
        static bool GetDllFullPath(string DllPath, ref Dictionary<string, ParamContainer> myParams, bool isChangeConfigDirectly)
        {
            string SoftwarePath;

            // 检验目录结构有效性
            if (!Directory.Exists(Path.GetDirectoryName(DllPath)))
            {
                // 文件夹结构已经发生改变，需要升级软件
                WriteLine($"[1]检测到目前版本的软件文件目录相较本软件开发时已发生改变，请联系软件开发者升级软件。\n");

                // 结束程序运行
                return false;
            }
            else { } // 一切正常，不做任何操作

            // 检验文件存在性
            if (!File.Exists(DllPath))
            {
                // 检查本地备份文件存在性
                if (!File.Exists(Path.Combine(LocalPath, ResourcesDir, DllName)))
                {
                    // 如果本地备份文件架处也不存在PCGameSDK.dll
                    WriteLine($"\n\n++== 注意！==++\n\n检测到本地不存在切换到Bilibili服务器所必须的DLL文件，请从网络/官方处获取文件[PCGameSDK.dll]" +
                              $"并将其保存在本软件所在目录的Resource文件夹下。\n\n" +
                              $"更为推荐的操作是，直接下载B服版本的软件，本软件可直接从中获取此文件。\n");

                    // 结束程序运行
                    return false;
                }
                else
                {
                    WriteLine($"\n\n++== 注意！==++\n\n检测到软件目录下不存在切换到Bilibili服务器所必须的DLL文件，将使用本地备份的dll文件。\n\n" +
                              $"此文件版本可能落后，如果软件运行后游戏运行异常，可参照本软件目录下Resource文件夹下的回退指导进行回退。\n");
                }
            }
            else { } // 一切正常，继续操作

            try
            {
                // 使用已经获得的DLL路径处理本地可能保存的DLL文件
                if (File.Exists(DllPath))
                {
                    DealLocalDll(DllPath);
                }

                // 从文件路径截取软件根目录所在的绝对路径
                // 文件PCGameSDK.dll所在位置是软件根目录下的 @"Genshin Impact Game\YuanShen_Data\Plugins\"
                DirectoryInfo TempInfo = new DirectoryInfo(DllPath.Replace(DllName, ""));
                SoftwarePath = TempInfo.Parent.Parent.Parent.FullName;

                // 检测路径有效性（这里主要是验证软件逻辑中包含的路径信息是否仍然有效）
                bool isValid = Directory.Exists(Path.Combine(SoftwarePath, DllDir))
                            && File.Exists(Path.Combine(SoftwarePath, ConfigDir, ConfigName));

                if (!isValid)
                {
                    // 文件夹结构已经发生改变，需要升级软件才能解决问题
                    WriteLine($"[2]检测到目前版本的软件文件目录相较本软件开发时已发生改变，请联系软件开发者升级软件。\n");

                    // 结束程序运行
                    return false;
                }

                bool isChange = false;

                // 检查本地文件中保存的设置
                if (isChangeConfigDirectly)
                {
                    // 如果要求不经询问直接把配置写入本地
                    isChange = true;
                }
                else if (!(myParams is null) && myParams.Count > 0)
                {
                    // 如果存在目标项，则取出目标值进行比对
                    if (myParams.ContainsKey(KEY_PATH) && myParams[KEY_PATH].ContainsKey(VALUE_SOFT_DIR))
                    {
                        // 检测本地参数与输入参数是否一致
                        if (myParams[KEY_PATH][VALUE_SOFT_DIR].value != SoftwarePath)
                        {
                            isChange = DialogResult.Yes == MessageBox.Show("检测到本地已存在软件路径配置，且与本次输入的信息不同。需要用本次获取到的信息覆盖它吗？",
                                                                           "提示", MessageBoxButtons.YesNo);
                        }
                        else { } // 如果参数一致，就什么都不做
                    }
                    else // 如果不存在此项，则添加此项
                    { isChange = true; }
                }
                else // 本地配置是空的，直接写入
                { isChange = true; }

                // TODO：将软件目录保存在本地
                if (isChange)
                {
                    if (myParams is null)
                    { myParams = new Dictionary<string, ParamContainer>(); }

                    // 检测存在性
                    if (!myParams.ContainsKey(KEY_PATH))
                    { myParams.Add(KEY_PATH, new ParamContainer(KEY_PATH, 0)); }

                    // 检测存在性并添加修改
                    if (myParams[KEY_PATH].ContainsKey(VALUE_SOFT_DIR))
                    {
                        myParams[KEY_PATH][VALUE_SOFT_DIR] = new Value(SoftwarePath, 0);
                    }
                    else
                    {
                        // 添加参数(index目前没用，所以暂且赋值为0。此值不会影响实际写入文件的内容。)
                        myParams[KEY_PATH].Add(VALUE_SOFT_DIR, new Value(SoftwarePath, 0));
                    }

                    // 执行写入操作（操作失败时直接返回）
                    if (!WriteConfigFile(Path.Combine(LocalPath, MyconfigName), myParams))
                    { return false; }

                    // 弹出提示
                    MessageBox.Show($"Done！本地配置参数已更新。", "OKK！");
                }
                else
                {
                    // 弹出提示
                    MessageBox.Show($"Done！本地配置参数维持原状。", "OKK！");
                }

                WriteLine("\n现在开始对当前参数的有效性进行验证...\n");

                Dictionary<string, ParamContainer> softwareParam = null;

                if (!CheckParams_Files(ref myParams, ref softwareParam))
                { return false; }
            }
            catch (Exception ex)
            {
                Write("\n" + ex.ToString());
                return false;
            }

            return true;
        }

        /// <summary>
        /// 从初始化文件中获取以键值对形式存在的参数集合
        /// </summary>
        /// <param name="fs">待读取的文件流</param>
        /// <returns>返回一个一键值对字典（集合）</returns>
        static Dictionary<string, ParamContainer> ReadConfigFile(string path)
        {
            // 以只读权限获得文件流
            FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read);

            // 从文件中读取的数据集合
            Dictionary<string, ParamContainer> Params = new Dictionary<string, ParamContainer>();

            // 获得文件的读取流
            StreamReader sr = new StreamReader(fs, Encoding.GetEncoding("utf-8"));

            // 中间变量
            string line, name, value, temp;
            long index = 0;
            ParamContainer TempPC = null;
            int tempIndex, lineCounter = 0;

            // 读取文件并初始化获得的参数
            while ((line = sr.ReadLine()) != null)
            {
                // 保存原读取结果
                temp = line;
                lineCounter++;

                // 跳过空行和注释行
                if (line == CRLF || line.First() == ';' || line.First() == '；')
                { }
                // 如果这是一个关键字行：关键字行的特征是，首尾为“[]”
                else if (line.First() == '[' && line.Last() == ']')
                {
                    // 取字符“[”后面一个字符的索引，也就是关键字字符串的第一个字符的索引
                    tempIndex = line.IndexOf("[") + 1;
                    name = line.Substring(tempIndex, line.IndexOf("]") - tempIndex);
                    TempPC = new ParamContainer(name, index);

                    // 为这个关键字创建一个与之对应的参数容器
                    Params.Add(name, TempPC);
                }
                // 如果这是一个参数行：参数行的特征是，存在至少一个“=”
                else if (line.Contains("="))
                {
                    // 参数名称是等号前的部分
                    name = line.Substring(0, line.IndexOf("="));
                    // 参数值是等号后的部分
                    value = line.Substring(line.IndexOf("=") + 1);

                    if (TempPC is null)
                    {
                        // 防止有的配置文件没写头，全是一个参数组合
                        TempPC = new ParamContainer("Globle", 0);
                    }
                    // 将关键字添加在对应的参数容器中
                    TempPC.Add(name, new Value(value, index));
                }
                else
                {
                    // 显示，但不做任何处理。
                    WriteLine($"读取文件{fs}时出现了格式不正确的行。行号：{lineCounter}，其内容为：\"{line}\"");
                }
                // 更新读取位置(要算上结尾的CRLF)
                index += temp.Length + CRLF.Length;
            }

            //Debug：检查文件占用情况
            //CheckFIleOccupancy(Path.Combine(ResourcesDir, ConfigName));

            // 释放资源
            sr.Close();
            fs.Close();

            return Params;
        }

        /// <summary>
        /// 将参数字典中的内容写入文件中
        /// </summary>
        /// <param name="path">字符串形式的文件路径</param>
        /// <param name="Params">参数容器字典</param>
        /// <returns>返回操作是否成功，true为成功</returns>
        static bool WriteConfigFile(string path, Dictionary<string, ParamContainer> Params)
        {
            // 获得文件流(覆写原文件)
            FileStream fs = File.Open(path, FileMode.Create, FileAccess.Write);
            // 获得文件的写入流
            StreamWriter sw = new StreamWriter(fs);

            // 将配置写入文件
            foreach (KeyValuePair<string, ParamContainer> pc in Params)
            {
                sw.Write("[" + pc.Value.Value + "]" + CRLF);
                foreach (KeyValuePair<string, Value> val in pc.Value)
                {
                    sw.Write(val.Key + "=" + val.Value.value + CRLF);
                }
            }
            // 释放资源
            sw.Close();
            fs.Close();

            return true;
        }

        /// <summary>
        /// 将登录模式切换为B服登录
        /// </summary>
        /// <param name="SoftwarePath">软件安装路径（含软件根文件夹）</param>
        /// <param name="Params">配置文件内容</param>
        /// <returns>返回操作是否成功，true为成功</returns>
        static bool SwitchToBiliBili(string SoftwarePath, ref Dictionary<string, ParamContainer> Params)
        {
            // 清空文件夹，备份原有文件以便回退
            if (Directory.Exists(Path.Combine(LocalPath, BackupAddr)))
            {
                Directory.Delete(Path.Combine(LocalPath, BackupAddr), true);
            }
            else { } // 什么也不做
            Directory.CreateDirectory(Path.Combine(LocalPath, BackupAddr));
            File.Copy(Path.Combine(SoftwarePath, ConfigDir, ConfigName), Path.Combine(LocalPath, BackupAddr, ConfigName));

            // 生成回退帮助文档
            // 获得文件流(覆写原文件)
            FileStream fs = File.Open(Path.Combine(LocalPath, BackupAddr, "回退指导.txt"), FileMode.Create, FileAccess.Write);
            // 获得文件的写入流
            StreamWriter sw = new StreamWriter(fs);

            //写入帮助文档内容：
            sw.WriteLine("++=== 手动回退指导 ===++\n");
            sw.WriteLine("\n1.将路径：「软件根路径」\\Genshin Impact Game\\YuanShen_Data\\Plugins下的文件「PCGameSDK.dll」移除；");
            sw.WriteLine("\n2.将路径：「软件根路径」\\Genshin Impact Game下的文件「config.ini」替换为本备份目录下对应名称的备份文件；");

            // 释放资源
            sw.Close(); fs.Close();

            // 将DLL拷贝到对应目录
            if (!File.Exists(Path.Combine(SoftwarePath, DllDir, DllName)))
            { File.Copy(Path.Combine(LocalPath, ResourcesDir, DllName), Path.Combine(SoftwarePath, DllDir, DllName)); }

            // 更改配置文件信道和服务器标签
            Params[AIM_KEY][AIM_VALUE_CHANNEL] = new Value("14", Params[AIM_KEY][AIM_VALUE_CHANNEL].index);
            Params[AIM_KEY][AIM_VALUE_CPS] = new Value("bilibili", Params[AIM_KEY][AIM_VALUE_CPS].index);

            // 将更改写入文件
            WriteConfigFile(Path.Combine(SoftwarePath, ConfigDir, ConfigName), Params);

            return true;
        }

        /// <summary>
        /// 将登录模式切换为官服登录
        /// </summary>
        /// <param name="SoftwarePath">软件安装路径（含软件根文件夹）</param>
        /// <param name="Params">配置文件内容</param>
        /// <returns>返回操作是否成功，true为成功</returns>
        static bool SwitchToMiHoYo(string SoftwarePath, ref Dictionary<string, ParamContainer> Params)
        {
            // 清空文件夹，备份原有文件以便回退
            if (Directory.Exists(Path.Combine(LocalPath, BackupAddr)))
            {
                Directory.Delete(Path.Combine(LocalPath, BackupAddr), true);
            }
            else { } // 什么也不做
            Directory.CreateDirectory(Path.Combine(LocalPath, BackupAddr));
            File.Copy(Path.Combine(SoftwarePath, ConfigDir, ConfigName), Path.Combine(Path.Combine(LocalPath, BackupAddr), ConfigName));
            File.Copy(Path.Combine(SoftwarePath, DllDir, DllName), Path.Combine(Path.Combine(LocalPath, BackupAddr), DllName));

            // 生成回退帮助文档
            // 获得文件流(覆写原文件)
            FileStream fs = File.Open(Path.Combine(Path.Combine(LocalPath, BackupAddr), "回退指导.txt"), FileMode.Create, FileAccess.Write);
            // 获得文件的写入流
            StreamWriter sw = new StreamWriter(fs);

            //写入帮助文档内容：
            sw.WriteLine("++=== 手动回退指导 ===++\n");
            sw.WriteLine("\n1.将本备份目录下的文件「PCGameSDK.dll」拷贝到路径：「软件根路径」\\Genshin Impact Game\\YuanShen_Data\\Plugins下；");
            sw.WriteLine("\n2.将路径：「软件根路径」\\Genshin Impact Game下的文件「config.ini」替换为本目录下对应名称的备份文件；");

            // 释放资源
            sw.Close(); fs.Close();

            // 移除对应的DLL(加个判断是为了防止预料之外的情况出现)
            if (File.Exists(Path.Combine(SoftwarePath, DllDir, DllName)))
            {
                File.Delete(Path.Combine(SoftwarePath, DllDir, DllName));
            }

            // 更改配置文件信道和服务器标签
            Params[AIM_KEY][AIM_VALUE_CHANNEL] = new Value("1", Params[AIM_KEY][AIM_VALUE_CHANNEL].index);
            Params[AIM_KEY][AIM_VALUE_CPS] = new Value("mihoyo", Params[AIM_KEY][AIM_VALUE_CPS].index);

            // 将更改写入文件
            WriteConfigFile(Path.Combine(SoftwarePath, ConfigDir, ConfigName), Params);

            return true;
        }

        /// <summary>
        /// 从系统注册表中搜索软件安装路径
        /// </summary>
        /// <remarks>
        /// 注意，本函数的思路是从“卸载”路径中截取软件安装路径。
        /// 因此如果uninstall程序不在软件安装目录下的话，此函数会失效，或者获得预料之外的结果。
        /// </remarks>
        /// <param name="UninstallName">待查找的软件名称（可在“控制面板/程序/卸载”中查找此名称，大小写敏感）</param>
        /// <param name="SoftRootName">软件本体的根目录名称（不是盘符），用于截取卸载路径中包含的安装路径</param>
        /// <param name="SoftwarePath">输出参数，用于存放找到的软件安装路径</param>
        /// <returns>标记查找是否成功，成功为true.</returns>
        static bool GetSoftwarePathFromReg(string UninstallName, string SoftRootName, out string SoftwarePath)
        {
            // 一般来说能卸载的软件都在这里有注册（除了免安装软件）
            string softKeyPath64 = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\";
            string softKeyPath32 = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\";
            bool isFinded = false;

            RegistryKey key = Registry.LocalMachine.OpenSubKey(softKeyPath64, false);

            // 赋予默认值
            SoftwarePath = null;

            // 看看注册表中有没有这一项
            if (key is null)
            {
                WriteLine("\n没有在64位注册表中搜索到\"Uninstall\"目录，可能是系统版本不兼容的问题。\n");
            }
            else
            {
                WriteLine($"\n开始检索64位注册表..." + "\n");
                //WriteLine($"\n此注册表项有[{key.SubKeyCount}]个项" + "\n");
                //int cnt = 0;
                foreach (string keyName in key.GetSubKeyNames())//遍历子项名称的字符串数组 
                {
                    RegistryKey subKey = key.OpenSubKey(keyName, false);//遍历子项节点

                    //cnt++;
                    //WriteLine($"项[{cnt}]: " + keyName + "\n");

                    if (subKey != null)
                    {
                        if ((string)(subKey.GetValue("DisplayName", "")) == UninstallName)
                        {
                            string UninstallString = (string)subKey.GetValue("UninstallString", null);
                            if (!(UninstallString is null) && UninstallString.Contains(SoftRootName))
                            {
                                // 从中截取软件安装路径
                                int CopyEndIndex = UninstallString.IndexOf(SoftRootName) + SoftRootName.Length;

                                SoftwarePath = UninstallString.Substring(0, CopyEndIndex);

                                isFinded = true;
                                return isFinded;
                            }
                            else { } // Do Nothing.
                        }
                        else { } // Do Nothing.
                    }
                }
            }

            key = Registry.LocalMachine.OpenSubKey(softKeyPath32, false);
            // 看看注册表中有没有这一项
            if (key is null)
            {
                WriteLine("\n没有在32位注册表中搜索到\"Uninstall\"目录，可能是系统版本不兼容的问题。\n");
            }
            else
            {
                WriteLine($"\n开始检索32位注册表..." + "\n");
                //WriteLine($"\n此注册表项有[{key.SubKeyCount}]个项" + "\n");
                //int cnt = 0;
                foreach (string keyName in key.GetSubKeyNames())//遍历子项名称的字符串数组 
                {
                    RegistryKey subKey = key.OpenSubKey(keyName, false);//遍历子项节点

                    //cnt++;
                    //WriteLine($"项[{cnt}]: " + keyName + "\n");

                    if (subKey != null)
                    {
                        if ((string)(subKey.GetValue("DisplayName", "")) == UninstallName)
                        {
                            string UninstallString = (string)subKey.GetValue("UninstallString", null);
                            if (!(UninstallString is null) && UninstallString.Contains(SoftRootName))
                            {
                                // 从中截取软件安装路径
                                int CopyEndIndex = UninstallString.IndexOf(SoftRootName) + SoftRootName.Length;

                                SoftwarePath = UninstallString.Substring(0, CopyEndIndex);

                                isFinded = true;
                                return isFinded;
                            }
                            else { } // Do Nothing.
                        }
                        else { } // Do Nothing.
                    }
                }
            }
            return isFinded;
        }

        /// <summary>
        /// 检查目标文件的占用情况，并打印到控制台上
        /// </summary>
        /// <param name="FIlePath" >待检查文件的路径</param>
        static void CheckFIleOccupancy(string FIlePath)
        {
            Process tool = new Process();
            tool.StartInfo.FileName = "handle.exe";
            tool.StartInfo.Arguments = FIlePath;
            tool.StartInfo.UseShellExecute = false;
            tool.StartInfo.RedirectStandardOutput = true;
            tool.Start();
            tool.WaitForExit();
            string outputTool = tool.StandardOutput.ReadToEnd();
            string matchPattern = @"(?<=\s+pid:\s+)\b(\d+)\b(?=\s+)";
            foreach (Match match in Regex.Matches(outputTool, matchPattern))
            {
                WriteLine($"\nID:[{int.Parse(match.Value)}] - [{Process.GetProcessById(int.Parse(match.Value))}]");
            }

            WriteLine("\n" + outputTool);
        }
    }
}
