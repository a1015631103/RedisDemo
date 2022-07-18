using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Text.Json;
using System.Text;
using System.Net;
using System.Linq;
namespace RedisDemo.Controllers
{
    /// <summary>
    /// Redis官方推荐的做法
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class RedisTest2Controller : ControllerBase
    {
        private readonly IConnectionMultiplexer redis;
        private readonly IDatabase db;

        public RedisTest2Controller(IConnectionMultiplexer redis)
        {
            this.redis = redis;
            db = redis.GetDatabase();
        }

        /// <summary>
        /// String测试
        /// </summary>
        /// <returns></returns>
        [HttpGet("StringTest")]
        public async Task<ResultData> StringTest()
        {
            ResultData result = new ResultData();

            //存取字符串
            bool stringExists = await db.KeyExistsAsync("stringTest");//判断key是否存在
            await db.StringSetAsync("stringTest", "stringTest", new TimeSpan(1, 0, 0));
            string stringResult = await db.StringGetAsync("stringTest");
            //也可以通过本方法获取返回值的byte[]，key和value底层其实将设置值转换成byte[]
            Lease<byte> lease = db.StringGetLease("stringTest");

            //批量设置字符串不能设置过期时间，可以参考事务用法来设置
            KeyValuePair<RedisKey, RedisValue>[] rrArray = new KeyValuePair<RedisKey, RedisValue>[3];
            byte[] tempBytes = Encoding.UTF8.GetBytes("1234567899");
            rrArray[0] = new KeyValuePair<RedisKey, RedisValue>("A1", "AAA");
            rrArray[1] = new KeyValuePair<RedisKey, RedisValue>("A2", 10);
            rrArray[2] = new KeyValuePair<RedisKey, RedisValue>("A3", tempBytes);
            await db.StringSetAsync(rrArray);
            RedisKey[] keyArray = new RedisKey[3] { "A1", "A2", "A3" };
            RedisValue[] valueArray = await db.StringGetAsync(keyArray);

            //计数
            await db.StringIncrementAsync("name1", 2);//加2
            await db.StringDecrementAsync("name1", 2);//减2

            //删除（可用于）
            //await db.KeyDeleteAsync("name1");

            result.Tag = 1;
            result.Message = "操作成功";
            return result;
        }

        /// <summary>
        /// hash测试
        /// </summary>
        /// <returns></returns>
        [HttpGet("HashTest")]
        public async Task<ResultData> HashTest()
        {
            ResultData result = new ResultData();

            await db.HashSetAsync("user", "age", 30);
            HashEntry[] hashEntityArray = new HashEntry[2] { new HashEntry("loginName", "testname"), new HashEntry("Birth", "2000-09-02") };
            await db.HashSetAsync("user", "loginName", "testname");
            await db.HashSetAsync("user", hashEntityArray);
            //获取List
            RedisValue[] keys = await db.HashKeysAsync("user");//获取所有field名称
            RedisValue[] values = await db.HashValuesAsync("user");//获取所有value
            HashEntry[] entrys = await db.HashGetAllAsync("user");//获取所有field-value

            //Hash也有计数
            await db.HashIncrementAsync("user", "age", 1);
            await db.HashDecrementAsync("user", "age", 2);

            //删除整个key和某个field-value
            //await db.KeyDeleteAsync("user");
            //await db.HashDeleteAsync("user", "age");

            result.Tag = 1;
            result.Message = "操作成功";
            return result;
        }

        /// <summary>
        /// List测试
        /// </summary>
        /// <returns></returns>
        [HttpGet("ListTest")]
        public async Task<ResultData> ListTest()
        {
            ResultData result = new ResultData();

            //可以结合起来做成简易的先进先出队列或后进先出
            List<Task<long>> listOfTasks = new List<Task<long>>();
            for (int i = 0; i < 10; i++)
            {
                listOfTasks.Add(db.ListRightPushAsync("rightList", $"测试{i + 1}的结果"));
            }
            await Task.WhenAll(listOfTasks);

            await db.ListLeftPopAsync("rightList");//结合上面，先进先出
            await db.ListRightPopAsync("rightList");//后进先出
            //长度
            long listLen = await db.ListLengthAsync("rightList");

            //也可以当作简易双链表进行一些操作
            await db.ListInsertBeforeAsync("rightList", "测试2的结果", "测试前面插入");
            await db.ListInsertAfterAsync("rightList", "测试2的结果", "测试后面插入");
            //获取整个列表(-1是最右边德下标)(左边是0到N-1，右边是-1到-N)
            RedisValue[] rangeArray = await db.ListRangeAsync("rightList", 0, -1);
            //获取指定某个下标值
            RedisValue indexValue = await db.ListGetByIndexAsync("rightList", 3);

            //删除指定元素(第三个参数==0删除所有，>0从左到右删除最多3个该值元素，<0从右到左删除最多3个该值元素
            await db.ListRemoveAsync("rightList", "测试后面插入", 3);
            await db.ListRemoveAsync("rightList", "测试前面插入", -3);

            //修改某个下标德值
            await db.ListSetByIndexAsync("rightList", 2, "测试");

            result.Tag = 1;
            result.Message = "操作成功";
            return result;
        }

        /// <summary>
        /// 不重复的集合测试
        /// </summary>
        /// <returns></returns>
        [HttpGet("SetTest")]
        public async Task<ResultData> SetTest()
        {
            ResultData result = new ResultData();
            //List<Task<bool>> listOfTasks = new List<Task<bool>>();
            //for (int i = 0; i < 10; i++)
            //{
            //    listOfTasks.Add(db.SetAddAsync("setList", $"测试{i}不重复德集合"));
            //}
            //await Task.WhenAll(listOfTasks);
            RedisValue[] setList = new RedisValue[10];


            for (int i = 0; i < 10; i++)
            {
                setList[i] = $"测试不重复集合{i}";
            }
            //批量添加元素到集合
            long value = await db.SetAddAsync("UserTest", setList);
            //获取某个集合的所有元素
            RedisValue[] belongValues = await db.SetMembersAsync("UserTest");
            //计算元素个数
            long count = await db.SetLengthAsync("UserTest");
            //判断元素是否在集合内
            bool isInList = await db.SetContainsAsync("UserTest", "测试不重复集合3");
            //随机从集合返回指定1个的元素（不会删除）
            RedisValue firstValue = await db.SetRandomMemberAsync("UserTest");
            //随机从集合返回指定多个的元素（不会删除）
            RedisValue[] muliValues = await db.SetRandomMembersAsync("UserTest", 4);
            //随机从集合返回指定1个的元素(会删除）
            RedisValue delAndReturnFirstValue = await db.SetPopAsync("UserTest");
            //随机从集合返回指定多个的元素（会删除）
            RedisValue[] delAndReturnMuliValues = await db.SetPopAsync("UserTest", 2);

            //如果元素太多可以用Scan命令，不需要自己操作游标,类库自己封装了，如果要分页可以用Take和Skip方法,需要安装System.Linq.Async包
            IAsyncEnumerable<RedisValue> returnValues = db.SetScanAsync(key: "UserTest", pattern: "测试不重复集合*");
            await foreach (var temp in returnValues)
            {

            }
            //获取第一页和第二页
            List<RedisValue> asyncFirstList = await returnValues.Skip(0).Take(2).ToListAsync();
            List<RedisValue> twoFirstList = await returnValues.Skip(2).Take(2).ToListAsync();

            RedisValue[] setListTwo = new RedisValue[7];
            for (int i = 0; i < 6; i++)
            {
                setList[i] = $"测试不重复集合{i}";
            }
            setListTwo[6] = "哈哈哈哈";
            long valueTwo = await db.SetAddAsync("UserTestTwo", setList);
            //求并，交，差集，返回结果
            await db.SetCombineAsync(SetOperation.Union, "UserTest", "UserTestTwo");
            await db.SetCombineAsync(SetOperation.Intersect, "UserTest", "UserTestTwo");
            await db.SetCombineAsync(SetOperation.Difference, "UserTest", "UserTestTwo");
            ////求并，交，差集，不返回结果，直接在Redis创建一个新的key来存储结果
            await db.SetCombineAndStoreAsync(SetOperation.Union, "UserTestUnion", "UserTest", "UserTestTwo");
            await db.SetCombineAndStoreAsync(SetOperation.Intersect, "UserTestIntersect", "UserTest", "UserTestTwo");
            await db.SetCombineAndStoreAsync(SetOperation.Difference, "UserTestDifference", "UserTest", "UserTestTwo");

            result.Tag = 1;
            result.Message = "获取成功";
            return result;
        }

        /// <summary>
        /// 不重复有序的集合测试
        /// </summary>
        /// <returns></returns>
        [HttpGet("SortedSetTest")]
        public async Task<ResultData> SortedSetTest()
        {
            ResultData result = new ResultData();
            SortedSetEntry[] ssEntityArray = new SortedSetEntry[10];
            for (int i = 0; i < 10; i++)
            {
                ssEntityArray[i] = new SortedSetEntry($"user{i}", i);
            }
            //添加
            long addValue = await db.SortedSetAddAsync("SortUser", ssEntityArray);
            //对score做增加和减少
            await db.SortedSetIncrementAsync("SortUser", "user3", 3);
            await db.SortedSetDecrementAsync("SortUser", "user3", 3);
            //获取集合长度和按照分数区别数量的长度
            long lenValue = await db.SortedSetLengthAsync("SortUser");
            long betweenLenValue = await db.SortedSetLengthByValueAsync("SortUser", 1, 5);
            //获取集合的部分元素(默认是score从低到高排序，开始序号是0，结尾开头序号是-1，如果后面3个参数都不设，就是获取全部值
            RedisValue[] allValues = await db.SortedSetRangeByRankAsync("SortUser", 1, -3, Order.Descending);
            //获取集合的所有元素包括值
            SortedSetEntry[] sseArray = await db.SortedSetRangeByRankWithScoresAsync("SortUser");
            //获取某个成员的分数
            double? scorebymember = await db.SortedSetScoreAsync("SortUser", "user7");
            //获取某个成员的排名
            long? rankValue = await db.SortedSetRankAsync("SortUser", "user7");

            result.Tag = 1;
            result.Message = "获取成功";
            return result;
        }

        /// <summary>
        /// 发布和订阅
        /// </summary>
        /// <returns></returns>
        [HttpGet("PubAndSub")]
        public void PubAndSubTest()
        {
            //Redis 发布订阅(pub/sub)是一种消息通信模式，可以用于消息的传输，Redis的发布订阅机制包括三个部分，发布者，订阅者和Channel。适宜做在线聊天、消息推送等。
            //发布者和订阅者都是Redis客户端，Channel则为Redis服务器端，发布者将消息发送到某个的频道，订阅了这个频道的订阅者就能接收到这条消息，客户端可以订阅任意数量的频道
            ISubscriber sub = redis.GetSubscriber();

            //订阅 Channel1 频道
            sub.Subscribe("Channel1", new Action<RedisChannel, RedisValue>((channel, message) =>
           {
               Console.WriteLine("Channel1" + " 订阅收到消息：" + message);
           }));
            List<Task<string>> listOfTasks = new List<Task<string>>();
            for (int i = 0; i < 10; i++)
            {
                sub.Publish("Channel1", "msg" + i);//向频道 Channel1 发送信息
                if (i == 2)
                {
                    sub.Unsubscribe("Channel1");//取消订阅
                }
            }
        }



        /// <summary>
        /// 事务测试
        /// </summary>
        [HttpGet("TransactionTest")]
        public void TransactionTest()
        {
            //事物开启后，会在调用 Execute 方法时把相应的命令操作封装成一个请求发送给 Redis 一起执行。
            string name = db.StringGet("name");
            string age = db.StringGet("age");

            //这里通过CreateTransaction函数（multi）来创建一个事物，调用其Execute函数（exec）提交事物。
            //其中的 "Condition.StringEqual("name", name)" 就相当于Redis命令中的watch name。
            var tran = db.CreateTransaction();//创建事物
            tran.AddCondition(Condition.StringEqual("name", name));//乐观锁
            tran.StringSetAsync("name", "海");
            tran.StringSetAsync("age", 25);
            db.StringSet("name", "Cang");//此时更改name值，提交事物的时候会失败。
            bool committed = tran.Execute();//提交事物，true成功，false回滚。
                                            //因为提交事物的过程中，name 值被修改，所以造成了回滚，所有给 name 赋值海，age 赋值25都失败了。
        }

        /// <summary>
        /// Batch批量测试
        /// </summary>
        [HttpGet("BatchTest")]
        public void BatchTest()
        {
            //batch会把所需要执行的命令打包成一条请求发到Redis，然后一起等待返回结果。减少网络开销。
            var batch = db.CreateBatch();

            //批量写
            Task t1 = batch.StringSetAsync("name", "羽");
            Task t2 = batch.StringSetAsync("age", 22);
            batch.Execute();
            Task.WaitAll(t1, t2);
            Console.WriteLine("Age:" + db.StringGet("age"));
            Console.WriteLine("Name:" + db.StringGet("name"));

            //批量写
            for (int i = 0; i < 100000; i++)
            {
                batch.StringSetAsync("age" + i, i);
            }
            batch.Execute();

            //批量读
            List<Task<RedisValue>> valueList = new List<Task<RedisValue>>();
            for (int i = 0; i < 10000; i++)
            {
                Task<RedisValue> tres = batch.StringGetAsync("age" + i);
                valueList.Add(tres);
            }
            batch.Execute();
            foreach (var redisValue in valueList)
            {
                string value = redisValue.Result;//取出对应的value值
            }
        }

        /// <summary>
        /// 分布式锁测试
        /// </summary>
        [HttpGet("DistributedLockTest")]
        public void DistributedLockTest()
        {
            //由于Redis是单线程模型，命令操作原子性，所以利用这个特性可以很容易的实现分布式锁。
            //lock_key表示的是redis数据库中该锁的名称，不可重复。 
            //token用来标识谁拥有该锁并用来释放锁。
            //TimeSpan表示该锁的有效时间。10秒后自动释放，避免死锁。
            RedisValue token = Environment.MachineName;
            if (db.LockTake("lock_key", token, TimeSpan.FromSeconds(10)))
            {
                try
                {
                    //TODO:开始做你需要的事情

                }
                finally
                {
                    db.LockRelease("lock_key", token);//释放锁
                }
            }
        }

        /// <summary>
        /// 位图测试
        /// </summary>
        /// <returns></returns>
        [HttpGet("BitMapTest")]
        public async Task<ResultData> BitMapTest()
        {
            ResultData result = new ResultData();
            //设置某个位的偏移位置值为1（设置的偏移量只支持正数从0开始,小于 2^32 (bit 映射被限制在 512 MB 之内，不像其他类型可以支持负数),
            await db.StringSetBitAsync("UserBit", 5, true);
            for (int i = 0; i < 20; i++)
            {
                if (i % 2 == 0)
                {
                    db.StringSetBit("UserBit", i, true);
                    db.StringSetBit("UserBit2", i, true);
                }
            }

            //获取某个位置的值
            bool bitValue = await db.StringGetBitAsync("UserBit", 5);
            //获取指定范围值为1的个数
            long countbit = await db.StringBitCountAsync("UserBit", 3, -3);
            //两个位集合的and（交集）,or(并集），not(非），xor（异或）
            await db.StringBitOperationAsync(Bitwise.And, "UserBitDesk", "UserBit", "UserBit2");
            //获取最小值为1的位置，或者获取某个范围内最小值为1的位置
            long povalue = await db.StringBitPositionAsync("UserBit", true, 0, -3);
            result.Tag = 1;
            result.Message = "测试成功";
            return result;
        }
    }
}
