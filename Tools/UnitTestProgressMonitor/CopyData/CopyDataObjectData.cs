// Ionic.CopyData, Version=1.0.1.0, Culture=neutral, PublicKeyToken=edbe51ad942a3f5c
// Ionic.CopyData.CopyDataObjectData
using System;

namespace Ionic.CopyData
{

[Serializable]
internal class CopyDataObjectData
{
    public string Channel;

    public object Data;

    public DateTime Sent;

    public CopyDataObjectData(object data, string channel)
    {
        Data = data;
        if (!data.GetType().IsSerializable)
        {
            throw new ArgumentException("Data object must be serializable.", "data");
        }
        Channel = channel;
        Sent = DateTime.Now;
    }
}

}
