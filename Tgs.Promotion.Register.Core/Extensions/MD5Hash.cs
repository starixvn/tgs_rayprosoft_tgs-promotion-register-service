using System.Security.Cryptography;
using System.Text;

namespace Tgs.Promotion.Register.Core.Extensions;

public class MD5Hash
{
    public static int HashToInt(string condition)
    {
        var md5Hasher = MD5.Create();
        var hashed = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(condition));
        return BitConverter.ToInt32(hashed, 0);
    }
}