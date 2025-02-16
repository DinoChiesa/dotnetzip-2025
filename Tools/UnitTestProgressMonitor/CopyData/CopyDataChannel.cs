using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Forms;
using Ionic.CopyData;

namespace Ionic.CopyData
{
public class CopyDataChannel : IDisposable
{
    private struct COPYDATASTRUCT
    {
        public IntPtr dwData;

        public int cbData;

        public IntPtr lpData;
    }

    private const int WM_COPYDATA = 74;

    private string channelName = "";

    private bool disposed = false;

    private NativeWindow owner = null;

    private bool recreateChannel = false;

    private DateTime lastSend;

    private static TimeSpan threshold = new TimeSpan(0, 0, 0, 0, 85);

    public string ChannelName => channelName;

    internal CopyDataChannel(NativeWindow owner, string channelName)
    {
        this.owner = owner;
        this.channelName = channelName;
        addChannel();
        lastSend = DateTime.FromFileTimeUtc(0L);
    }

    private void addChannel()
    {
        SetProp(owner.Handle, channelName, (int)owner.Handle);
    }

    public void Dispose()
    {
        if (!disposed)
        {
            if (channelName.Length > 0)
            {
                removeChannel();
            }
            channelName = "";
            disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    ~CopyDataChannel()
    {
        Dispose();
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }
        return Equals((CopyDataChannel)obj);
    }

    public bool Equals(CopyDataChannel cdc)
    {
        if (cdc == null)
        {
            return false;
        }
        return owner.Handle == cdc.owner.Handle && channelName.Equals(cdc.channelName);
    }

    public override int GetHashCode()
    {
        return (int)((uint)(int)owner.Handle ^ channelName.GetHashCode());
    }

    public void OnHandleChange()
    {
        removeChannel();
        recreateChannel = true;
    }

    private void removeChannel()
    {
        RemoveProp(owner.Handle, channelName);
    }

    public int Send(object obj)
    {
        int num = 0;
        if (disposed)
        {
            throw new InvalidOperationException("Object has been disposed");
        }
        if (recreateChannel)
        {
            addChannel();
        }
        CopyDataObjectData graph = new CopyDataObjectData(obj, channelName);
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        MemoryStream memoryStream = new MemoryStream();
        binaryFormatter.Serialize(memoryStream, graph);
        memoryStream.Flush();
        DateTime utcNow = DateTime.UtcNow;
        while (utcNow - lastSend < threshold)
        {
            Thread.Sleep(15);
            utcNow = DateTime.UtcNow;
        }
        lastSend = utcNow;
        int num2 = (int)memoryStream.Length;
        if (num2 > 0)
        {
            byte[] array = new byte[num2];
            memoryStream.Seek(0L, SeekOrigin.Begin);
            memoryStream.Read(array, 0, num2);
            IntPtr intPtr = Marshal.AllocCoTaskMem(num2);
            Marshal.Copy(array, 0, intPtr, num2);
            EnumWindows enumWindows = new EnumWindows();
            enumWindows.GetWindows();
            foreach (EnumWindowsItem item in enumWindows.Items)
            {
                if (!item.Handle.Equals(owner.Handle) && GetProp(item.Handle, channelName) != 0)
                {
                    COPYDATASTRUCT lParam = default(COPYDATASTRUCT);
                    lParam.cbData = num2;
                    lParam.dwData = IntPtr.Zero;
                    lParam.lpData = intPtr;
                    int num3 = SendMessage(item.Handle, 74, (int)owner.Handle, ref lParam);
                    num += ((Marshal.GetLastWin32Error() == 0) ? 1 : 0);
                }
            }
            Marshal.FreeCoTaskMem(intPtr);
        }
        memoryStream.Close();
        return num;
    }

    [DllImport("user32", CharSet = CharSet.Auto)]
    private static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, ref COPYDATASTRUCT lParam);

    [DllImport("user32", CharSet = CharSet.Auto)]
    private static extern int SetProp(IntPtr hwnd, string lpString, int hData);

    [DllImport("user32", CharSet = CharSet.Auto)]
    private static extern int GetProp(IntPtr hwnd, string lpString);

    [DllImport("user32", CharSet = CharSet.Auto)]
    private static extern int RemoveProp(IntPtr hwnd, string lpString);
}

}
