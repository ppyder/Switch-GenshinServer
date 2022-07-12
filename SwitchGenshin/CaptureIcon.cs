using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Console;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;
//using System.Diagnostics;
//using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SwitchGenshin
{
    ///   <summary>  
    ///  获取文件系统中对象的信息，例如：文件、文件夹、驱动器根目录 
    ///   </summary>  
    public class MyFileInfo
    {
        [DllImport("shell32.dll", EntryPoint = "SHGetFileInfo")]
        public static extern int GetFileInfo(string pszPath, int dwFileAttributes,
                                             ref FileInfomation psfi, int cbFileInfo, int uFlags);

        private MyFileInfo() { }

        [StructLayout(LayoutKind.Sequential)]
        public struct FileInfomation
        {
            public IntPtr hIcon;
            public int iIcon;
            public int dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }
        public enum FileAttributeFlags : int
        {
            FILE_ATTRIBUTE_READONLY = 0x00000001,
            FILE_ATTRIBUTE_HIDDEN = 0x00000002,
            FILE_ATTRIBUTE_SYSTEM = 0x00000004,
            FILE_ATTRIBUTE_DIRECTORY = 0x00000010,
            FILE_ATTRIBUTE_ARCHIVE = 0x00000020,
            FILE_ATTRIBUTE_DEVICE = 0x00000040,
            FILE_ATTRIBUTE_NORMAL = 0x00000080,
            FILE_ATTRIBUTE_TEMPORARY = 0x00000100,
            FILE_ATTRIBUTE_SPARSE_FILE = 0x00000200,
            FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400,
            FILE_ATTRIBUTE_COMPRESSED = 0x00000800,
            FILE_ATTRIBUTE_OFFLINE = 0x00001000,
            FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x00002000,
            FILE_ATTRIBUTE_ENCRYPTED = 0x00004000
        }
        public enum GetFileInfoFlags : int
        {
            SHGFI_ICON = 0x000000100,  //  get icon
            SHGFI_DISPLAYNAME = 0x000000200,  //  get display name
            SHGFI_TYPENAME = 0x000000400,  //  get type name
            SHGFI_ATTRIBUTES = 0x000000800,  //  get attributes
            SHGFI_ICONLOCATION = 0x000001000,  //  get icon location
            SHGFI_EXETYPE = 0x000002000,  //  return exe type
            SHGFI_SYSICONINDEX = 0x000004000,  //  get system icon index
            SHGFI_LINKOVERLAY = 0x000008000,  //  put a link overlay on icon
            SHGFI_SELECTED = 0x000010000,  //  show icon in selected state
            SHGFI_ATTR_SPECIFIED = 0x000020000,  //  get only specified attributes
            SHGFI_LARGEICON = 0x000000000,  //  get large icon
            SHGFI_SMALLICON = 0x000000001,  //  get small icon
            SHGFI_OPENICON = 0x000000002,  //  get open icon
            SHGFI_SHELLICONSIZE = 0x000000004,  //  get shell size icon
            SHGFI_PIDL = 0x000000008,  //  pszPath is a pidl
            SHGFI_USEFILEATTRIBUTES = 0x000000010,  //  use passed dwFileAttribute
            SHGFI_ADDOVERLAYS = 0x000000020,  //  apply the appropriate overlays
            SHGFI_OVERLAYINDEX = 0x000000040   //  Get the index of the overlay
        }

        ///   <summary>  
        ///  通过路径获取小图标 
        ///   </summary>  
        ///   <param name="path"> 文件或文件夹路径 </param>  
        ///   <returns> 获取的图标 </returns>  
        public static Icon GetSmallIcon(string path)
        {
            FileInfomation _info = new FileInfomation();

            GetFileInfo(path, 0, ref _info, Marshal.SizeOf(_info),
            (int)(GetFileInfoFlags.SHGFI_ICON | GetFileInfoFlags.SHGFI_SHELLICONSIZE));
            try
            {
                return Icon.FromHandle(_info.hIcon);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 获取文件的图标索引号
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>图标索引号</returns>
        //public static int GetIconIndex(string fileName)
        //{
        //    FileInfomation info = new FileInfomation();
        //    IntPtr iconIntPtr = new IntPtr(GetFileInfo(fileName, 0, ref info, Marshal.SizeOf(info), 
        //                                   (int)(GetFileInfoFlags.SHGFI_SYSICONINDEX | GetFileInfoFlags.SHGFI_OPENICON)));
        //    if (iconIntPtr == IntPtr.Zero)
        //        return -1;
        //    return info.iIcon;
        //}

        /// <summary>
        /// 根据图标索引号获取图标
        /// </summary>
        /// <param name="iIcon">图标索引号</param>
        /// <param name="flag">图标尺寸标识</param>
        /// <returns></returns>
        //public static Icon GetIcon(int iIcon, GetFileInfoFlags flag)
        //{

        //    IImageList list = null;
        //    Guid theGuid = new Guid(IID_IImageList);//目前所知用IID_IImageList2也是一样的
        //    SHGetImageList(flag, ref theGuid, ref list);//获取系统图标列表
        //    IntPtr hIcon = IntPtr.Zero;
        //    int r = list.GetIcon(iIcon, ILD_TRANSPARENT | ILD_IMAGE, ref hIcon);//获取指定索引号的图标句柄
        //    return System.Drawing.Icon.FromHandle(hIcon);
        //}

        /// <summary>
        ///  方法3：从文件获取Icon图标
        /// </summary>
        /// <param name="fileName">文件名称</param>
        /// <param name="flag">图标尺寸标识</param>
        /// <returns></returns>
        //public static Icon GetIconFromFile(string fileName, GetFileInfoFlags flag)
        //{
        //    return GetIcon(GetIconIndex(fileName), flag);
        //}
    }
}