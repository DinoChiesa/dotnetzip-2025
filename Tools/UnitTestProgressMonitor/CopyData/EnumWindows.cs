// Ionic.CopyData, Version=1.0.1.0, Culture=neutral, PublicKeyToken=edbe51ad942a3f5c
// Ionic.CopyData.EnumWindows
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Ionic.CopyData;

namespace Ionic.CopyData
{

public class EnumWindows
{
    private delegate int EnumWindowsProc(IntPtr hwnd, int lParam);

    private class NativeMethods
    {
        [DllImport("user32")]
        public static extern int EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, int lParam);

        [DllImport("user32")]
        public static extern int EnumWindows(EnumWindowsProc lpEnumFunc, int lParam);
    }

    private List<EnumWindowsItem> items;

    public ReadOnlyCollection<EnumWindowsItem> Items => items.AsReadOnly();

    public void GetWindows()
    {
        items = new List<EnumWindowsItem>();
        NativeMethods.EnumWindows(WindowEnum, 0);
    }

    public void GetWindows(IntPtr hWndParent)
    {
        items = new List<EnumWindowsItem>();
        NativeMethods.EnumChildWindows(hWndParent, WindowEnum, 0);
    }

    protected virtual bool OnWindowEnum(IntPtr hWnd)
    {
        items.Add(new EnumWindowsItem(hWnd));
        return true;
    }

    private int WindowEnum(IntPtr hWnd, int lParam)
    {
        if (OnWindowEnum(hWnd))
        {
            return 1;
        }
        return 0;
    }
}
}
