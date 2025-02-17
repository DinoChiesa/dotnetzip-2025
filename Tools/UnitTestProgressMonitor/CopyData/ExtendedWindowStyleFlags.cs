// Ionic.CopyData, Version=1.0.1.0, Culture=neutral, PublicKeyToken=edbe51ad942a3f5c
// Ionic.CopyData.ExtendedWindowStyleFlags
using System;

namespace Ionic.CopyData
{

[Flags]
public enum ExtendedWindowStyleFlags
{
    WS_EX_ACCEPTFILES = 0x10,
    WS_EX_APPWINDOW = 0x40000,
    WS_EX_CLIENTEDGE = 0x200,
    WS_EX_COMPOSITED = 0x2000000,
    WS_EX_CONTEXTHELP = 0x400,
    WS_EX_CONTROLPARENT = 0x10000,
    WS_EX_DLGMODALFRAME = 1,
    WS_EX_LAYERED = 0x80000,
    WS_EX_LAYOUTRTL = 0x400000,
    WS_EX_LEFT = 0,
    WS_EX_LEFTSCROLLBAR = 0x4000,
    WS_EX_LTRREADING = 0,
    WS_EX_MDICHILD = 0x40,
    WS_EX_NOACTIVATE = 0x8000000,
    WS_EX_NOINHERITLAYOUT = 0x100000,
    WS_EX_NOPARENTNOTIFY = 4,
    WS_EX_RIGHT = 0x1000,
    WS_EX_RIGHTSCROLLBAR = 0,
    WS_EX_RTLREADING = 0x2000,
    WS_EX_STATICEDGE = 0x20000,
    WS_EX_TOOLWINDOW = 0x80,
    WS_EX_TOPMOST = 8,
    WS_EX_TRANSPARENT = 0x20,
    WS_EX_WINDOWEDGE = 0x100
}
}
