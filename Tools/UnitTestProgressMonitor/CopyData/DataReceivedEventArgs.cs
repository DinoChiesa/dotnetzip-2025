// Ionic.CopyData, Version=1.0.1.0, Culture=neutral, PublicKeyToken=edbe51ad942a3f5c
// Ionic.CopyData.DataReceivedEventArgs
using System;

namespace Ionic.CopyData
{

public class DataReceivedEventArgs : EventArgs
{
    private string channelName = "";

    private object data = null;

    private DateTime received;

    private DateTime sent;

    public string ChannelName => channelName;

    public object Data => data;

    public DateTime Received => received;

    public DateTime Sent => sent;

    internal DataReceivedEventArgs(string channelName, object data, DateTime sent)
    {
        this.channelName = channelName;
        this.data = data;
        this.sent = sent;
        received = DateTime.Now;
    }
}
}
