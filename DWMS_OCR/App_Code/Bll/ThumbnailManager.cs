﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace DWMS_OCR.App_Code.Bll
{
    class ThumbnailManager : IDisposable
    {
        #region ShellFolder Enumerations
        [Flags]
        private enum ESTRRET : int
        {
            STRRET_WSTR = 0x0000,
            STRRET_OFFSET = 0x0001,
            STRRET_CSTR = 0x0002
        }
        [Flags]
        private enum ESHCONTF : int
        {
            SHCONTF_FOLDERS = 32,
            SHCONTF_NONFOLDERS = 64,
            SHCONTF_INCLUDEHIDDEN = 128
        }

        [Flags]
        private enum ESHGDN : int
        {
            SHGDN_NORMAL = 0,
            SHGDN_INFOLDER = 1,
            SHGDN_FORADDRESSBAR = 16384,
            SHGDN_FORPARSING = 32768
        }
        [Flags]
        private enum ESFGAO : int
        {
            SFGAO_CANCOPY = 1,
            SFGAO_CANMOVE = 2,
            SFGAO_CANLINK = 4,
            SFGAO_CANRENAME = 16,
            SFGAO_CANDELETE = 32,
            SFGAO_HASPROPSHEET = 64,
            SFGAO_DROPTARGET = 256,
            SFGAO_CAPABILITYMASK = 375,
            SFGAO_LINK = 65536,
            SFGAO_SHARE = 131072,
            SFGAO_READONLY = 262144,
            SFGAO_GHOSTED = 524288,
            SFGAO_DISPLAYATTRMASK = 983040,
            SFGAO_FILESYSANCESTOR = 268435456,
            SFGAO_FOLDER = 536870912,
            SFGAO_FILESYSTEM = 1073741824,
            SFGAO_HASSUBFOLDER = -2147483648,
            SFGAO_CONTENTSMASK = -2147483648,
            SFGAO_VALIDATE = 16777216,
            SFGAO_REMOVABLE = 33554432,
            SFGAO_COMPRESSED = 67108864
        }
        #endregion

        #region IExtractImage Enumerations
        private enum EIEIFLAG
        {
            IEIFLAG_ASYNC = 0x0001,
            IEIFLAG_CACHE = 0x0002,
            IEIFLAG_ASPECT = 0x0004,
            IEIFLAG_OFFLINE = 0x0008,
            IEIFLAG_GLEAM = 0x0010,
            IEIFLAG_SCREEN = 0x0020,
            IEIFLAG_ORIGSIZE = 0x0040,
            IEIFLAG_NOSTAMP = 0x0080,
            IEIFLAG_NOBORDER = 0x0100,
            IEIFLAG_QUALITY = 0x0200
        }
        #endregion

        #region ShellFolder Structures
        [StructLayoutAttribute(LayoutKind.Sequential, Pack = 4, Size = 0, CharSet = CharSet.Auto)]
        private struct STRRET_CSTR
        {
            public ESTRRET uType;
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 520)]
            public byte[] cStr;
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Auto)]
        private struct STRRET_ANY
        {
            [FieldOffset(0)]
            public ESTRRET uType;
            [FieldOffset(4)]
            public IntPtr pOLEString;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        private struct SIZE
        {
            public int cx;
            public int cy;
        }
        #endregion

        #region Com Interop for IUnknown
        [ComImport, Guid("00000000-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IUnknown
        {
            [PreserveSig]
            IntPtr QueryInterface(ref Guid riid, out IntPtr pVoid);

            [PreserveSig]
            IntPtr AddRef();

            [PreserveSig]
            IntPtr Release();
        }
        #endregion

        #region COM Interop for IEnumIDList
        [ComImportAttribute()]
        [GuidAttribute("000214F2-0000-0000-C000-000000000046")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IEnumIDList
        {
            [PreserveSig]
            int Next(
             int celt,
             ref IntPtr rgelt,
             out int pceltFetched);

            void Skip(
             int celt);

            void Reset();

            void Clone(
             ref IEnumIDList ppenum);
        };
        #endregion

        #region COM Interop for IShellFolder
        [ComImportAttribute()]
        [GuidAttribute("000214E6-0000-0000-C000-000000000046")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellFolder
        {
            void ParseDisplayName(
             IntPtr hwndOwner,
             IntPtr pbcReserved,
             [MarshalAs(UnmanagedType.LPWStr)] string lpszDisplayName,
             out int pchEaten,
             out IntPtr ppidl,
             out int pdwAttributes
             );

            void EnumObjects(
             IntPtr hwndOwner,
             [MarshalAs(UnmanagedType.U4)] ESHCONTF grfFlags,
             ref IEnumIDList ppenumIDList
             );

            void BindToObject(
             IntPtr pidl,
             IntPtr pbcReserved,
             ref Guid riid,
             ref IShellFolder ppvOut
             );

            void BindToStorage(
             IntPtr pidl,
             IntPtr pbcReserved,
             ref Guid riid,
             IntPtr ppvObj
             );

            [PreserveSig]
            int CompareIDs(
             IntPtr lParam,
             IntPtr pidl1,
             IntPtr pidl2
             );

            void CreateViewObject(
             IntPtr hwndOwner,
             ref Guid riid,
             IntPtr ppvOut
             );

            void GetAttributesOf(
             int cidl,
             IntPtr apidl,
             [MarshalAs(UnmanagedType.U4)] ref ESFGAO rgfInOut
             );

            void GetUIObjectOf(
             IntPtr hwndOwner,
             int cidl,
             ref IntPtr apidl,
             ref Guid riid,
             out int prgfInOut,
             ref IUnknown ppvOut
             );

            void GetDisplayNameOf(
             IntPtr pidl,
             [MarshalAs(UnmanagedType.U4)] ESHGDN uFlags,
             ref STRRET_CSTR lpName
             );

            void SetNameOf(
             IntPtr hwndOwner,
             IntPtr pidl,
             [MarshalAs(UnmanagedType.LPWStr)] string lpszName,
             [MarshalAs(UnmanagedType.U4)] ESHCONTF uFlags,
             ref IntPtr ppidlOut
             );

        };

        #endregion

        #region COM Interop for IExtractImage
        [ComImportAttribute()]
        [GuidAttribute("BB2E617C-0920-11d1-9A0B-00C04FC2D6C1")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IExtractImage
        {
            void GetLocation(
             [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszPathBuffer,
             int cch,
             ref int pdwPriority,
             ref SIZE prgSize,
             int dwRecClrDepth,
             ref int pdwFlags
             );

            void Extract(
             out IntPtr phBmpThumbnail
             );
        }
        #endregion

        #region UnmanagedMethods for IShellFolder
        private class UnmanagedMethods
        {
            [DllImport("ole32", CharSet = CharSet.Auto)]
            internal extern static void CoTaskMemFree(IntPtr ptr);

            [DllImport("shell32", CharSet = CharSet.Auto)]
            internal extern static int SHGetDesktopFolder(out IShellFolder ppshf);

            [DllImport("shell32", CharSet = CharSet.Auto)]
            internal extern static int SHGetPathFromIDList(IntPtr pidl, StringBuilder pszPath);

            [DllImport("gdi32", CharSet = CharSet.Auto)]
            internal extern static int DeleteObject(IntPtr hObject);
        }
        #endregion

        #region Member Variables
        private bool disposed = false;
        private System.Drawing.Bitmap thumbNail = null;
        #endregion

        #region Implementation
        public System.Drawing.Bitmap ThumbNail
        {
            get
            {
                return thumbNail;
            }
        }

        public System.Drawing.Bitmap GetThumbnail(string file, int width, int height)
        {
            if ((!File.Exists(file)) && (!Directory.Exists(file)))
            {
                throw new FileNotFoundException(
                 String.Format("The file '{0}' does not exist", file),
                 file);
            }

            if (thumbNail != null)
            {
                thumbNail.Dispose();
                thumbNail = null;
            }

            IShellFolder folder = null;
            try
            {
                folder = GetDesktopFolder;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            if (folder != null)
            {
                IntPtr pidlMain = IntPtr.Zero;
                try
                {
                    int cParsed = 0;
                    int pdwAttrib = 0;
                    string filePath = Path.GetDirectoryName(file);
                    pidlMain = IntPtr.Zero;
                    folder.ParseDisplayName(
                     IntPtr.Zero,
                     IntPtr.Zero,
                     filePath,
                     out cParsed,
                     out pidlMain,
                     out pdwAttrib);
                }
                catch (Exception ex)
                {
                    Marshal.ReleaseComObject(folder);
                    throw ex;
                }

                if (pidlMain != IntPtr.Zero)
                {
                    // IShellFolder:
                    Guid iidShellFolder = new Guid("000214E6-0000-0000-C000-000000000046");
                    IShellFolder item = null;

                    try
                    {
                        folder.BindToObject(pidlMain, IntPtr.Zero, ref
       iidShellFolder, ref item);
                    }
                    catch (Exception ex)
                    {
                        Marshal.ReleaseComObject(folder);
                        UnmanagedMethods.CoTaskMemFree(pidlMain);
                        throw ex;
                    }

                    if (item != null)
                    {
                        IEnumIDList idEnum = null;
                        try
                        {
                            item.EnumObjects(
                             IntPtr.Zero,
                             (ESHCONTF.SHCONTF_FOLDERS |
                             ESHCONTF.SHCONTF_NONFOLDERS),
                             ref idEnum);
                        }
                        catch (Exception ex)
                        {
                            Marshal.ReleaseComObject(folder);
                            UnmanagedMethods.CoTaskMemFree(pidlMain);
                            throw ex;
                        }

                        if (idEnum != null)
                        {
                            int hRes = 0;
                            IntPtr pidl = IntPtr.Zero;
                            int fetched = 0;
                            bool complete = false;
                            while (!complete)
                            {
                                hRes = idEnum.Next(1, ref pidl, out fetched);
                                if (hRes != 0)
                                {
                                    pidl = IntPtr.Zero;
                                    complete = true;
                                }
                                else
                                {
                                    if (GetThumbnail(file, pidl, item, width, height))
                                    {
                                        complete = true;
                                    }
                                }
                                if (pidl != IntPtr.Zero)
                                {
                                    UnmanagedMethods.CoTaskMemFree(pidl);
                                }
                            }

                            Marshal.ReleaseComObject(idEnum);
                        }


                        Marshal.ReleaseComObject(item);
                    }

                    UnmanagedMethods.CoTaskMemFree(pidlMain);
                }

                Marshal.ReleaseComObject(folder);
            }
            return thumbNail;
        }

        private bool GetThumbnail(string file, IntPtr pidl, IShellFolder item, int width, int height)
        {
            IntPtr hBmp = IntPtr.Zero;
            IExtractImage extractImage = null;

            try
            {
                string pidlPath = PathFromPidl(pidl);
                if (Path.GetFileName(pidlPath).ToUpper().Equals(Path.GetFileName(file).ToUpper()))
                {
                    IUnknown iunk = null;
                    int prgf = 0;
                    Guid iidExtractImage = new Guid("BB2E617C-0920-11d1-9A0B-00C04FC2D6C1");
                    item.GetUIObjectOf(IntPtr.Zero, 1, ref pidl, ref iidExtractImage, out prgf, ref iunk);
                    extractImage = (IExtractImage)iunk;

                    if (extractImage != null)
                    {
                        SIZE sz = new SIZE();
                        sz.cx = width;
                        sz.cy = height;
                        StringBuilder location = new StringBuilder(260, 260);
                        int priority = 0;
                        int requestedColourDepth = 32;
                        EIEIFLAG flags = EIEIFLAG.IEIFLAG_ASPECT | EIEIFLAG.IEIFLAG_SCREEN;
                        int uFlags = (int)flags;

                        /* 2012-10-09, try catch added to handle the Error
                         * The data necessary to complete this operation is not yet available. (Exception from HRESULT: 0x8000000A)
                         */
                        try
                        {
                            extractImage.GetLocation(location, location.Capacity, ref priority, ref sz, requestedColourDepth, ref uFlags);
                        }
                        catch { }
                        extractImage.Extract(out hBmp);
                        if (hBmp != IntPtr.Zero)
                        {
                            thumbNail = System.Drawing.Bitmap.FromHbitmap(hBmp);
                        }

                        Marshal.ReleaseComObject(extractImage);
                        extractImage = null;
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (hBmp != IntPtr.Zero)
                {
                    UnmanagedMethods.DeleteObject(hBmp);
                }
                if (extractImage != null)
                {
                    Marshal.ReleaseComObject(extractImage);
                }
                throw ex;
            }
        }

        private string PathFromPidl(IntPtr pidl)
        {
            StringBuilder path = new StringBuilder(260, 260);
            int result = UnmanagedMethods.SHGetPathFromIDList(pidl, path);
            if (result == 0)
            {
                return string.Empty;
            }
            else
            {
                return path.ToString();
            }
        }

        private IShellFolder GetDesktopFolder
        {
            get
            {
                IShellFolder ppshf;
                int r = UnmanagedMethods.SHGetDesktopFolder(out ppshf);
                return ppshf;
            }
        }
        #endregion

        #region Constructor, Destructor, Dispose
        public ThumbnailManager()
        {
        }

        public void Dispose()
        {
            if (!disposed)
            {
                if (thumbNail != null)
                {
                    thumbNail.Dispose();
                }
                disposed = true;
            }
        }

        ~ThumbnailManager()
        {
            Dispose();
        }
        #endregion
    }
}
