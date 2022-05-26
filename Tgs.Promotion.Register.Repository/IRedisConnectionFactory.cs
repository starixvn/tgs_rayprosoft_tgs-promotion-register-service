using StackExchange.Redis;

namespace Tgs.Promotion.Register.Repository;

public interface IRedisConnectionFactory
{
    public IDatabase GetDataBase();
}