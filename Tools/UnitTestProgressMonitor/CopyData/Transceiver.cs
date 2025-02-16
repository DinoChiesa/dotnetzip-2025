// Ionic.CopyData, Version=1.0.1.0, Culture=neutral, PublicKeyToken=edbe51ad942a3f5c
// Ionic.CopyData.Transceiver
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using Ionic.CopyData;

namespace Ionic.CopyData
{

public class Transceiver : NativeWindow, IDisposable
{
    private struct COPYDATASTRUCT
    {
        public IntPtr dwData;

        public int cbData;

        public IntPtr lpData;
    }

    private const int WM_COPYDATA = 74;

    private const int WM_DESTROY = 2;

    private CopyDataChannel _channel;

    private string _channelName;

    private bool disposed;

    public string Channel
    {
        get
        {
            return _channelName;
        }
        set
        {
            _channelName = value;
            _channel = ((value == null) ? null : new CopyDataChannel(this, value));
        }
    }

    public bool CanSend => _channelName != null;

    public event EventHandler<DataReceivedEventArgs> DataReceived;

    protected void OnDataReceived(DataReceivedEventArgs e)
    {
        this.DataReceived(this, e);
    }

    protected override void OnHandleChange()
    {
        if (_channel != null)
        {
            _channel.OnHandleChange();
        }
        base.OnHandleChange();
    }

    public void Send(object msg)
    {
        if (_channelName == null || _channel == null)
        {
            throw new InvalidOperationException();
        }
        _channel.Send(msg);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 74)
        {
            if (_channel != null)
            {
                COPYDATASTRUCT cOPYDATASTRUCT = default(COPYDATASTRUCT);
                cOPYDATASTRUCT = (COPYDATASTRUCT)Marshal.PtrToStructure(m.LParam, typeof(COPYDATASTRUCT));
                if (cOPYDATASTRUCT.cbData > 0)
                {
                    byte[] array = new byte[cOPYDATASTRUCT.cbData];
                    Marshal.Copy(cOPYDATASTRUCT.lpData, array, 0, cOPYDATASTRUCT.cbData);
                    MemoryStream serializationStream = new MemoryStream(array);
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    CopyDataObjectData copyDataObjectData = (CopyDataObjectData)binaryFormatter.Deserialize(serializationStream);
                    if (_channelName == copyDataObjectData.Channel)
                    {
                        DataReceivedEventArgs e = new DataReceivedEventArgs(copyDataObjectData.Channel, copyDataObjectData.Data, copyDataObjectData.Sent);
                        OnDataReceived(e);
                        m.Result = (IntPtr)1;
                    }
                }
            }
        }
        else if (m.Msg == 2)
        {
            OnHandleChange();
        }
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (!disposed)
        {
            if (_channel != null)
            {
                _channel.Dispose();
            }
            _channel = null;
            disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    ~Transceiver()
    {
        Dispose();
    }
}
}
