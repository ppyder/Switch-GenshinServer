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
    /// <summary>
    /// 注意，此程序需要将生成选项选为“x64”，否则查找注册表的操作可能会失败。
    /// </summary>
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

        #region 预定义路径信息
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
        #endregion

        #region 提示信息/帮助文本
        /// <summary>
        /// 提示信息文本：需要手动初始化
        /// </summary>
        private const string Tips_ManuallyInit = "\n---\n通过注册表在本地查询软件安装路径失败。需要手动生成配置文件，请遵照提示执行操作。\n" +
                                                 "\n软件需要进行手动初始化:\n" +
                                                 "\n未在本地找到软件安装路径信息，请首先确认是否已经安装了原神。\n" +
                                                 "\n++=====++\n" +
                                                 "执行手动初始化操作：\n" +
                                                 "1. 找到软件所在文件夹；\n" +
                                                 "2. 将文件夹图标拖动到本软件图标上（使用本软件打开文件夹）；\n" +
                                                 "3. 如果出现了预料之外的报错或软件闪退，可联系软件作者对其进行排错和升级。:)";

        /// <summary>
        /// 提示信息文本：切换服务器成功
        /// </summary>
        private const string Tips_SwitchSuccessfully = "\n++=====++\n" +
                            "其他提示（如果切换后游戏运行不正常）：\n" +
                            "1. 可通过本软件目录下的Resource\\Backup文件夹中的文件进行回退， 注意阅读其中的回退指导！\n" +
                            "2. 如果希望继续使用本软件进行快捷切换，可联系软件作者对其进行升级。:)";

        /// <summary>
        /// 提示信息文本：目录结构以改变
        /// </summary>
        private const string Tips_CatalogueChanged = "\n\n++== 注意！==++\n\n检测到目前版本的软件文件目录相较本软件开发时已发生改变，请联系软件开发者升级软件。\n";

        /// <summary>
        /// 提示信息文本：当前软件使用的DLL和本地保存的dll文件不一致
        /// </summary>
        private const string Tips_DllNotMatched = "目标软件目录下的dll文件与本地保存的dll文件不一致（可能版本更新或本地文件损坏），需要用它替换本地备份文件吗？";

        /// <summary>
        /// 提示信息文本：软件目录下的Config文件版本与本地备份不一致
        /// </summary>
        private const string Tips_CfgNotMatched = "目标软件目录下的Config文件与本地保存的存档文件不一致（可能版本更新或本地文件损坏），需要用它替换本地备份文件吗？";

        /// <summary>
        /// 提示信息文本：当前软件使用的config文件中不包含软件运行所需的工作参数
        /// </summary>
        private const string Tips_CfgNotValid = "\n\n++== 注意！==++\n\n检测到软件目录下的配置文件中不存在本软件工作所需的参数，可能有如下情形：\n\n" +
                                                  "1.如果上次成功运行本软件后软件并未更新过，则可能是配置文件损坏，可通过本软件目录下的Resource文件夹中的备份文件进行恢复，" +
                                                  "注意阅读其中的回退指导！\n\n" +
                                                  "2.目前版本下软件配置文件的工作逻辑相较本软件开发时已发生改变。若如此，请联系软件开发者升级软件。\n";

        /// <summary>
        /// 提示信息文本：本地和软件目录下均不存在实现功能需要的dll文件
        /// </summary>
        private const string Tips_DllNotExist = "\n\n++== 注意！==++\n\n检测到本地不存在切换到Bilibili服务器所必须的DLL文件，请从网络/官方/本人Git站点处获取文件[PCGameSDK.dll]" +
                                                "并将其保存在本软件所在目录的Resource文件夹下。\n\n" +
                                                "更为推荐的操作是，直接下载B服版本的软件，本软件可直接从中获取此文件。\n";

        /// <summary>
        /// 提示信息文本：本地存在实现功能需要的dll文件，但软件目录下没有
        /// </summary>
        private const string Tips_DllNotExistInSoftDir = "\n\n++== 注意！==++\n\n检测到软件目录下不存在切换到Bilibili服务器所必须的DLL文件，将使用本地备份的dll文件。\n\n" +
                                                         "此文件版本可能落后，如果软件运行后游戏运行异常，可参照本软件目录下Resource文件夹下的回退指导进行回退。\n";

        /// <summary>
        /// 询问信息文本：是否将本地保存的旧文件备份保存？
        /// </summary>
        private const string Question_IsSaveOld = "是否保留旧文件（将作为备份保存在本软件根目录下）？";
        #endregion

        #region 预定义标签
        private const string KEY_PATH = "Path";         // 路径信息标签
        private const string VALUE_SOFT_DIR = "SoftwareDir";

        private const string AIM_KEY = "General";       // 执行切换时的关键字标签（参数组名）
        private const string AIM_VALUE_CHANNEL = "channel";
        private const string AIM_VALUE_CPS = "cps";
        #endregion

        static void Main(string[] args)
        {
            /// 暂时想到的是全部写入的方法。
            /// 如果是大文件，这种将文件全部内容读到内存里的做法不可取，需要研究一下。
            /// 尤其是文件中间插入文字的办法。

            // 初始化程序（初始化失败即报错，且用输出参数获取到本地配置文件内容）
            if (!ProgramInit(args, out Dictionary<string, ParamContainer> MyParams, out Dictionary<string, ParamContainer> SoftwareParams))
            { MessageBox.Show("请留意控制台输出的提示信息。", "Opps!"); return; }

            // 获取本地保存的软件路径
            string SoftwareDir = MyParams[KEY_PATH][VALUE_SOFT_DIR].value;

            // 查询软件当前状态(1.PCGameSDK.dll存在，2.config.ini中的[General]channel = 14，cps = bilibili即为bilibil登录)
            bool isBilibili = File.Exists(Path.Combine(SoftwareDir, DllDir, DllName)) 
                              && ( SoftwareParams[AIM_KEY][AIM_VALUE_CHANNEL].value == "14" 
                                   && SoftwareParams[AIM_KEY][AIM_VALUE_CPS].value == "bilibili" );

            // 获取表示当前服务器的字符串
            string CurrentServer = (isBilibili ? "Bilibili" : "Mihoyo");

            // 询问是否切换并执行
            DialogResult isToSwitch = MessageBox.Show($"目前的服务器选项是 [{CurrentServer}]，确定要切换吗？", "确认操作", MessageBoxButtons.YesNo);

            if (DialogResult.Yes == isToSwitch)
            {
                if(!SwitchServer(isBilibili, SoftwareDir, ref SoftwareParams))
                { MessageBox.Show("请留意控制台输出的错误信息。", "Opps!"); return; }
            }
            WriteLine("\n程序正常结束。");

            return;
        }

        /// <summary>
        /// 完成程序的前置设置和路径检查、建立工作
        /// </summary>
        /// <param name="args">程序从运行环境中获得的参数组</param>
        /// <param name="myParams">本软件的配置文件参数</param>
        /// <returns>返回代表能否正常初始化程序的bool变量，一切正常为true</returns>
        static bool ProgramInit(string[] args, 
                                out Dictionary<string, ParamContainer> myParams,
                                out Dictionary<string, ParamContainer> softParams)
        {
            // 标记当前参数是否已经被检查过
            bool isChecked = false;
            softParams = null;

            //设置窗口标题
            Console.Title = "Genshin登录服务器切换  #=== Powered by ppyder ++ Mail: 942041771@qq.com ===#";

            // 基本信息显示
            WriteLine("\n++ Genshin登录服务器切换程序 ++\n\n#=== Powered by ppyder ++ Mail: 942041771@qq.com ===#\n");
            WriteLine($"\n---\n当前软件的工作路径（进程所在路径）为：{Environment.CurrentDirectory}" +
                      $"\n当前软件的启动路径（软件本体所在路径）为：{Application.StartupPath}\n");

            // 将工作路径重新设置为软件本体所在路径，方便操作，也更安全。
            Environment.CurrentDirectory = Application.StartupPath;
            WriteLine($"\n---\n已将工作路径重定向为：{Environment.CurrentDirectory}\n");

            // 初始化资源路径
            if (Directory.Exists(Path.Combine(LocalPath, ResourcesDir)))
            { WriteLine("\n---\n检测到资源目录已存在。\n"); }
            else
            { WriteLine("\n---\n检测到资源目录不存在，即将创建。\n"); }

            // 创建资源目录（如果目录存在则不创建，返回已存在的目录信息）
            DirectoryInfo ResDirInfo = Directory.CreateDirectory(Path.Combine(LocalPath, ResourcesDir));
            WriteLine($"资源目录的绝对路径为：{ResDirInfo.FullName}\n");

            // 创建初始化文件，如果存在则不创建(创建完成后利用其返回的实例将文件关闭，释放资源)
            if (!File.Exists(Path.Combine(LocalPath, MyconfigName)))
            { File.Create(Path.Combine(LocalPath, MyconfigName)).Close(); }

            #region 提取图标的测试代码
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

            // 应对可能存在的手动初始化操作
            if (args.Length > 0)
            {
                if (!ModifyProgramManually(args, out myParams, out softParams))
                { return false; }
                else
                { isChecked = true; }
            }
            else // 读取本地配置信息
            {
                myParams = ReadConfigFile(Path.Combine(LocalPath, MyconfigName));
            }

            // 看看是否需要对软件参数进行自动初始化
            if (myParams is null || 0 == myParams.Count)
            {
                WriteLine("\n---\n检测到本地配置不存在，尝试在注册表中查找软件安装信息...\n");

                // 尝试进行自动初始化
                if (!ModifyProgramAutomatically(out myParams, out softParams))
                { return false; }
            }
            else if(!isChecked) // 进行参数有效性检查
            {
                if (!CheckParams_Files(ref myParams, out softParams, false))
                { return false; }
            }
            return true;
        }

        /// <summary>
        /// 获取软件安装路径后的操作
        /// </summary>
        /// <param name="path">获取到的路径</param>
        /// <param name="myParams">待初始化的本地配置参数，输出参数</param>
        /// <param name="softParams">待读取的软件配置参数，输出参数</param>
        /// <returns>如果一切正常，参数也合法，软件可以正常运行，则返回true</returns>
        static bool GottenSoftwarePath(string path, 
                                       out Dictionary<string, ParamContainer> myParams,
                                       out Dictionary<string, ParamContainer> softParams)
        {
            myParams = new Dictionary<string, ParamContainer>();
            myParams.Add(KEY_PATH, new ParamContainer(KEY_PATH, 0));

            // 添加参数(index目前没用，所以暂且赋值为0。此值不会影响实际写入文件的内容。)
            myParams[KEY_PATH].Add(VALUE_SOFT_DIR, new Value(path, 0));

            // 检测参数有效性,如果有效且与本地存档不一致，则直接覆盖本地存档
            if (!CheckParams_Files(ref myParams, out softParams, true))
            { return false; }

            // 执行写入操作（操作失败时直接返回）
            if (!WriteConfigFile(Path.Combine(LocalPath, MyconfigName), myParams))
            { return false; }

            // 弹出提示
            MessageBox.Show($"Done！本地配置参数已更新。", "OKK！");

            return true;
        }

        /// <summary>
        /// 支持手动初始化软件（赋予软件安装路径）功能
        /// </summary>
        /// <param name="args">程序从运行环境获取的输入参数</param>
        /// <param name="myParams">待初始化的配置文件参数</param>
        /// <returns>初始化操作是否正常执行，true为正常</returns>
        static bool ModifyProgramManually(string[] args, 
                                          out Dictionary<string, ParamContainer> myParams,
                                          out Dictionary<string, ParamContainer> softParams)
        {
            myParams = null;
            softParams = null;

            foreach (string arg in args)
            {
                WriteLine("已获取到参数：" + arg);

                if (arg.EndsWith(DllName))
                {
                    // 从文件路径截取软件根目录所在的绝对路径
                    // 文件PCGameSDK.dll所在位置是软件根目录下的 @"Genshin Impact Game\YuanShen_Data\Plugins\"
                    DirectoryInfo TempInfo = new DirectoryInfo(arg.Replace(DllName, ""));
                    string SoftwarePath = TempInfo.Parent.Parent.Parent.FullName;

                    if (!GottenSoftwarePath(SoftwarePath, out myParams, out softParams))
                    { return false; }
                }
                else if (arg.EndsWith(SoftwareDirName))
                {
                    // 直接把文件目录加上按照上述思路解析信息
                    if (!GottenSoftwarePath(arg, out myParams, out softParams))
                    { return false; }
                }
                else { } // 其他输入不做处理
            }
            return true;
        }

        /// <summary>
        /// 自动初始化程序配置参数
        /// </summary>
        /// <param name="myParams">本地配置文件内容（待更改/填充）</param>
        /// <returns>返回表征操作执行成功的bool变量，true代表成功</returns>
        static bool ModifyProgramAutomatically(out Dictionary<string, ParamContainer> myParams,
                                               out Dictionary<string, ParamContainer> softParams)
        {
            myParams = null;
            softParams = null;

            if (GetSoftwarePathFromReg("原神", SoftwareDirName, out string SoftPath))
            {
                WriteLine("\n已在注册表中查找到软件安装路径：" + SoftPath + "\n");

                // 直接把文件目录加上，按照前述思路解析信息
                if (!GottenSoftwarePath(SoftPath, out myParams, out softParams))
                { return false; }
            }
            else // 未能自动初始化，提示用户手动对软件进行一次初始化
            { WriteLine(Tips_ManuallyInit); return false; }

            return true;
        }

        /// <summary>
        /// 检查配置文件中是否包含本软件工作所需的参数
        /// </summary>
        /// <param name="filePath">配置文件路径</param>
        /// <param name="fileParams">如果文件存在，将在此变量中储存该文件中的信息</param>
        /// <returns>返回检查结果，true代表包含工作所需参数</returns>
        static bool CheckConfigFile(string filePath, out Dictionary<string, ParamContainer> fileParams)
        {
            // 赋予初值
            fileParams = null;

            if (File.Exists(filePath))
            {
                // 从文件中读取信息
                fileParams = ReadConfigFile(filePath);

                // （注意顺序不能错，这里利用了逻辑运算时的截断特性：当左边的表达式足以决定表达式结果时，后续将不进行下一步的运算）
                return fileParams != null && fileParams.Keys.Contains(AIM_KEY)
                    && fileParams[AIM_KEY].Keys.Contains(AIM_VALUE_CHANNEL) && fileParams[AIM_KEY].Keys.Contains(AIM_VALUE_CPS);
            }
            else  // 本地备份文件不存在
            { return false; }
        }

        /// <summary>
        /// 询问用户如何处理与本地备份不一致的文件
        /// </summary>
        /// <param name="tips">提示文本</param>
        /// <param name="softPath">软件目录下的目标文件路径</param>
        /// <param name="localPath">本地备份目录下的目标文件路径</param>
        /// <returns>返回是否变更了本地备份，true为已变更</returns>
        static bool DealNotMatchedFile(string tips, string softPath, string localPath, bool isChangeDirectly)
        {
            // 文件不匹配，根据询问是否需要保存新的作为以后使用的文件
            DialogResult isChange = isChangeDirectly ? DialogResult.Yes : MessageBox.Show(tips, "提示", MessageBoxButtons.YesNo);

            if (DialogResult.Yes == isChange)
            {
                // 询问是否需要保留旧版本的文件
                DialogResult isSave = MessageBox.Show(Question_IsSaveOld, "提示", MessageBoxButtons.YesNo);
                if (DialogResult.Yes == isSave)
                {
                    // 获取文件后缀
                    string FileEnd = DateTime.Now.ToString().Replace("/", "-").Replace(" ", "_").Replace(":", "-") + ".save";

                    // 改名保存
                    File.Copy(localPath, Path.Combine(localPath + "_Local_" + FileEnd));

                    // 删除原文件
                    File.Delete(localPath);
                    WriteLine("\n已将文件改名备份到本地。\n");
                }
                else // 直接删除
                {
                    File.Delete(localPath);
                }
                // 将新文件拷贝到本地留存以待后用
                File.Copy(softPath, localPath);
            }
            else { } //什么也不做，继续使用本地保存的文件 

            return DialogResult.Yes == isChange;
        }

        /// <summary>
        /// 比对本地和软件目录下的无关参数以确认版本信息，并予以处理
        /// </summary>
        /// <param name="softwarePath">原神安装路径</param>
        /// <param name="backParams">本地备份文件中的参数字典</param>
        /// <param name="softParams">软件目录下的参数字典</param>
        static void CompareConfigFile(string softwarePath,
                                      Dictionary<string, ParamContainer> backParams,
                                      ref Dictionary<string, ParamContainer> softParams,
                                      bool isChangeDirectly)
        {
            // ExceptParams用于存储已经检查过的键值对以便下次检查时跳过，用于提升程序性能
            Dictionary<string, ParamContainer> ExceptParams = new Dictionary<string, ParamContainer>();

            // 添加比对的排除项（因为这两项是要改变的）
            ExceptParams.Add(AIM_KEY, new ParamContainer(AIM_KEY));
            ExceptParams[AIM_KEY].Add(AIM_VALUE_CHANNEL, new Value());
            ExceptParams[AIM_KEY].Add(AIM_VALUE_CPS, new Value());

            WriteLine("\n\n---\n开始检查原神的启动配置文件...\n");
            WriteLine("\n以「目标文件」为基准对「备份文件」进行参数匹配性检查...\n");

            // 按照soft中有的参数，比对back中的参数
            bool isEqual = CompareParamsEqual(softParams, backParams, ref ExceptParams);

            WriteLine("\n以「备份文件」为基准对「目标文件」进行参数匹配性检查（不会再显示已经检出的差异项）...\n");

            // 由于可能存在back中有、soft中不存在的参数，所以还要换一下基准检查一次
            isEqual &= CompareParamsEqual(backParams, softParams, ref ExceptParams);

            // 根据匹配结果决定处理方式
            if (!isEqual)
            {
                WriteLine("\nDone.查出以上差异，备份配置文件版本一致性验证完成。\n");

                bool isChanged = DealNotMatchedFile( Tips_CfgNotMatched,
                                                     Path.Combine(softwarePath, ConfigDir, ConfigName),
                                                     Path.Combine(LocalPath, ResourcesDir, ConfigName),
                                                     isChangeDirectly);
                if (!isChanged)
                {
                    //继续使用本地保存的文件,并将软件参数替换为本地保存参数
                    softParams = backParams;
                    WriteLine("\nConfig文件保持原状，本次软件运行也将按照本地备份文件指定的方式工作。\n");
                }
            }
            else
            { WriteLine("\nDone.二者版本内容一致，备份配置文件版本一致性验证完成。\n"); }
        }

        /// <summary>
        /// 对自身配置文件以及本地备份文件进行检查校验，并做相应处理
        /// </summary>
        /// <param name="myParams">从本地配置文件中读取出来的引用参数</param>
        /// <param name="SoftParams">从配置文件中读取出来的输出参数</param>
        /// <param name="isChangeDirectly">标记是否不经询问直接覆盖本地存档，是为true</param>
        /// <returns>返回合法性检查的结果，参数有效返回true。</returns>
        static bool CheckParams_Files(ref Dictionary<string, ParamContainer> myParams,
                                      out Dictionary<string, ParamContainer> softParams, bool isChangeDirectly)
        {
            // 初始化输出参数
            softParams = null;

            // 从参数组合中获取软件安装路径
            string SoftwarePath = myParams is null ? "" : myParams[KEY_PATH][VALUE_SOFT_DIR].value;

            // 检测软件路径有效性
            if (Directory.Exists(SoftwarePath))
            { WriteLine("\n---\n软件安装路径有效性验证完成。\n"); }
            else
            {
                WriteLine("\n---\n检测到本地保存的软件安装路径无效，尝试在注册表中查找软件安装信息...\n");

                // 尝试进行自动初始化
                if (!ModifyProgramAutomatically(out myParams, out softParams))
                { return false; }
            }

            // 检验软件文件夹结构有效性
            if (Directory.Exists(Path.Combine(SoftwarePath, DllDir)) && File.Exists(Path.Combine(SoftwarePath, ConfigDir, ConfigName)))
            { WriteLine("\n---\n软件目录结构有效性验证完成。\n"); }
            else
            { WriteLine(Tips_CatalogueChanged); return false; }

            // 检验目标目录下Dll文件是否版本一致，并进行相应处理
            if (File.Exists(Path.Combine(SoftwarePath, DllDir, DllName)))
            { DealLocalDll(Path.Combine(SoftwarePath, DllDir, DllName), isChangeDirectly); }
            else
            {
                // 检查本地备份文件存在性
                if (!File.Exists(Path.Combine(LocalPath, ResourcesDir, DllName)))
                { WriteLine(Tips_DllNotExist); return false; }
                else
                { WriteLine(Tips_DllNotExistInSoftDir); }
            }

            // 首先检查目标文件中是否存在本软件功能实现所需的信息
            if(!CheckConfigFile(Path.Combine(SoftwarePath, ConfigDir, ConfigName), out softParams))
            { 
                WriteLine(Tips_CfgNotValid);
                return false;
            }

            // 本地备份文件中的参数字典
            Dictionary<string, ParamContainer> BackParams = null;

            // 其次检查本地备份文件中是否存在本软件功能实现所需的信息
            if (!File.Exists(Path.Combine(LocalPath, ResourcesDir, ConfigName)))
            { WriteLine("\n---\n检测到本地未备份配置文件，将从软件安装目录备份此文件...\n"); }
            else if (!CheckConfigFile(Path.Combine(LocalPath, ResourcesDir, ConfigName), out BackParams))
            {
                // 不存在本软件逻辑实现所需的参数，给出提示
                WriteLine("\n---\n检测到本地备份的配置文件中不存在本软件工作所需的参数，可能是此配置文件已损坏，将删除此文件并重新备份...\n");

                // 移除已损坏的备份文件，重新置空备份文件参数引用
                File.Delete(Path.Combine(LocalPath, ResourcesDir, ConfigName));
                BackParams = null;
            }

            // 如果有效文件不存在，就直接拷贝一个变更前的副本到本地进行备份
            if (BackParams is null)
            { 
                // 将新文件拷贝到本地留存以待后用
                File.Copy(Path.Combine(SoftwarePath, ConfigDir, ConfigName), Path.Combine(LocalPath, ResourcesDir, ConfigName));

                // 提示
                WriteLine("\n已将目标配置文件备份到本地。\n");
            }
            else // 若存在，则检查备份配置文件版本信息
            {
                CompareConfigFile(SoftwarePath, BackParams, ref softParams, isChangeDirectly);
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
        static void DealLocalDll(string DllPath, bool isChangeDirectly)
        {
            if (File.Exists(Path.Combine(LocalPath, ResourcesDir, DllName)))
            {
                // 以MD5校验工具校验文件是否一致
                if (!IsFileEqual(Path.Combine(LocalPath, ResourcesDir, DllName), DllPath))
                {
                    // 文件不匹配，
                    DealNotMatchedFile(Tips_DllNotMatched, DllPath, Path.Combine(LocalPath, ResourcesDir, DllName), isChangeDirectly);
                }
                else
                { WriteLine("\n---\n校验完成：目标dll文件与本地保存的dll文件版本一致。"); }
            }
            else
            {
                // 直接把目标文件拷贝到本地以待后用
                File.Copy(DllPath, Path.Combine(LocalPath, ResourcesDir, DllName));

                WriteLine("\n已将目标dll文件备份到本地。");
            }
            return;
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
        /// 切换服务器
        /// </summary>
        /// <param name="isBilibili">标记当前服务器是否是哔哩哔哩的bool标签</param>
        /// <param name="softwareDir">原神安装路径</param>
        /// <param name="softwareParams">原神启动服务器相关的配置参数</param>
        /// <returns></returns>
        static bool SwitchServer(bool isBilibili, string softwareDir, ref Dictionary<string, ParamContainer> softwareParams)
        {
            // 使用委托来简化软件逻辑
            SwitchLogic Switcher = isBilibili ? new SwitchLogic(SwitchToMiHoYo) : new SwitchLogic(SwitchToBiliBili);
            string AnotherServer = (isBilibili ? "Mihoyo" : "Bilibili");

            try // 尝试执行切换任务
            { Switcher(softwareDir, ref softwareParams); }
            catch (Exception ex)
            { Write("\n" + ex.ToString()); return false; }

            //根据标志给出提示
            MessageBox.Show($"Done！已切换为 [{AnotherServer}] 服务器登录！\n" + Tips_SwitchSuccessfully, "玩得开心！");
            return true;
        }

        /// <summary>
        /// 委托类型：切换服务器的操作
        /// </summary>
        /// <param name="SoftwarePath">软件安装路径（含软件根文件夹）</param>
        /// <param name="Params">配置文件内容</param>
        /// <returns>返回操作是否成功，true为成功</returns>
        delegate void SwitchLogic(string SoftwarePath, ref Dictionary<string, ParamContainer> Params);

        /// <summary>
        /// 将登录模式切换为B服登录
        /// </summary>
        /// <param name="SoftwarePath">软件安装路径（含软件根文件夹）</param>
        /// <param name="Params">配置文件内容</param>
        /// <returns>返回操作是否成功，true为成功</returns>
        static void SwitchToBiliBili(string SoftwarePath, ref Dictionary<string, ParamContainer> Params)
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
        }

        /// <summary>
        /// 将登录模式切换为官服登录
        /// </summary>
        /// <param name="SoftwarePath">软件安装路径（含软件根文件夹）</param>
        /// <param name="Params">配置文件内容</param>
        /// <returns>返回操作是否成功，true为成功</returns>
        static void SwitchToMiHoYo(string SoftwarePath, ref Dictionary<string, ParamContainer> Params)
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
        }

        /// <summary>
        /// 从系统注册表中搜索软件安装路径
        /// </summary>
        /// <remarks>
        /// 注意，本函数的思路是从“卸载”路径中截取软件安装路径。
        /// 因此如果uninstall程序不在软件安装目录下的话，此函数会失效，或者获得预料之外的结果。
        /// </remarks>
        /// <param name="uninstallName">待查找的软件名称（可在“控制面板/程序/卸载”中查找此名称，大小写敏感）</param>
        /// <param name="softRootName">软件本体的根目录名称（不是盘符），用于截取卸载路径中包含的安装路径</param>
        /// <param name="softwarePath">输出参数，用于存放找到的软件安装路径</param>
        /// <returns>标记查找是否成功，成功为true.</returns>
        static bool GetSoftwarePathFromReg(string uninstallName, string softRootName, out string softwarePath)
        {
            // 赋予默认值
            softwarePath = null;

            // 一般来说能卸载的软件都在这里有注册（除了免安装软件）
            const string SoftKeyPath64 = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\";
            const string SoftKeyPath32 = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\";

            // 为简化程序结构而构造的遍历数组
            string[] softKeys = new string[2] { SoftKeyPath64, SoftKeyPath32 };
            string[] softKeyNames = new string[2] { "x64", "x86" };

            // 注册表顶级节点
            RegistryKey key;

            // 检索这两个根节点下的所有项以查询目标软件
            for (int i = 0; i < softKeys.Length; i++)
            {
                key = Registry.LocalMachine.OpenSubKey(softKeys[i], false);

                // 看看注册表中有没有这一项
                if (key is null)
                {
                    WriteLine($"\n没有在{softKeyNames[i]}注册表中搜索到\"Uninstall\"目录，可能是系统版本不兼容的问题。\n");
                }
                else
                {
                    WriteLine($"\n开始检索{softKeyNames[i]}注册表..." + "\n");
                    //WriteLine($"\n此注册表项有[{key.SubKeyCount}]个项" + "\n");
                    //int cnt = 0;
                    foreach (string keyName in key.GetSubKeyNames())//遍历子项名称的字符串数组 
                    {
                        RegistryKey subKey = key.OpenSubKey(keyName, false);//遍历子项节点

                        //cnt++;
                        //WriteLine($"项[{cnt}]: " + keyName + "\n");

                        if (subKey != null)
                        {
                            if ((string)(subKey.GetValue("DisplayName", "")) == uninstallName)
                            {
                                string UninstallString = (string)subKey.GetValue("UninstallString", null);
                                if (!(UninstallString is null) && UninstallString.Contains(softRootName))
                                {
                                    // 从中截取软件安装路径
                                    int CopyEndIndex = UninstallString.IndexOf(softRootName) + softRootName.Length;

                                    softwarePath = UninstallString.Substring(0, CopyEndIndex);
                                    return true;
                                }
                                else { } // Do Nothing.
                            }
                            else { } // Do Nothing.
                        }
                    }
                }

            }
            return false;
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
