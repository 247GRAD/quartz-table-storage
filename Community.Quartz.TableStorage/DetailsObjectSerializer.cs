using System.Text;
using Community.Quartz.TableStorage.Entities;
using Quartz.Spi;

namespace Community.Quartz.TableStorage;

/// <summary>
/// Points the <see cref="IObjectSerializer"/> to the <see cref="Details"/> serialization, which is JSON based. This
/// serialized data is then encoded using <see cref="Encoding.UTF8"/>. This avoids issues with deprecated serialization
/// APIs that were removed due to security concerns.
/// </summary>
public class DetailsObjectSerializer : IObjectSerializer
{
    public void Initialize()
    {
    }

    public byte[] Serialize<T>(T obj) where T : class =>
        Encoding.UTF8.GetBytes(Details.Serialize(obj));

    public T DeSerialize<T>(byte[] data) where T : class =>
        Details.Deserialize<T>(Encoding.UTF8.GetString(data));
}