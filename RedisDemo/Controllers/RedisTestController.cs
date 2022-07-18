using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using RedisDemo.Extends;
using RedisDemo.Helper;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace RedisDemo.Controllers
{
    /// <summary>
    /// 微软官方推荐的做法
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class RedisTestController : ControllerBase
    {
        private readonly IDistributedCache _cache;

        public RedisTestController(IDistributedCache cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// 用微软官方推荐的接口测试存储和读取
        /// </summary>
        /// <returns></returns>
        [HttpGet("FirstTest")]
        public ResultData<List<FirstTestModel>> FirstTest()
        {
            ResultData<List<FirstTestModel>> result = new ResultData<List<FirstTestModel>>();
            #region 初始化FirstTestModel
            DateTime dtnow = DateTime.Now;
            List<FirstTestModel> list = new List<FirstTestModel>();

            list.Add(new FirstTestModel()
            {
                Guid = Guid.NewGuid(),
                CreateTime = dtnow,
                Sex = 3,
                Age = 10,
                FirstName = "第一个Redis的测试1",
                LastName = "最后的名字中文1"
            });
            list.Add(new FirstTestModel()
            {
                Guid = Guid.NewGuid(),
                CreateTime = dtnow,
                Sex = 1,
                Age = 15,
                FirstName = "第一个Redis的测试2",
                LastName = "最后的名字中文2"
            });
            list.Add(new FirstTestModel()
            {
                Guid = Guid.NewGuid(),
                CreateTime = dtnow,
                Sex = 2,
                Age = 20,
                FirstName = "第一个Redis的测试3",
                LastName = "最后的名字中文3"
            });
            list.Add(new FirstTestModel()
            {
                Guid = Guid.NewGuid(),
                CreateTime = dtnow,
                Sex = 2,
                Age = 30,
                FirstName = "第一个Redis的测试4",
                LastName = "最后的名字中文4"
            });
            list.Add(new FirstTestModel()
            {
                Guid = Guid.NewGuid(),
                CreateTime = dtnow,
                Sex = 1,
                Age = 50,
                FirstName = "第一个Redis的测试5",
                LastName = "最后的名字中文5"
            });

            #endregion

            string testStr = JsonExtensions.SerializeCustom<List<FirstTestModel>>(list);
            byte[] testBytes = UTF8Encoding.UTF8.GetBytes(testStr);
            _cache.Set("FirstTest", testBytes, new DistributedCacheEntryOptions() { SlidingExpiration = new TimeSpan(2, 0, 0) });


            byte[] getBytes = _cache.Get("FirstTest");
            string RepoValue = Encoding.UTF8.GetString(getBytes);
            List<FirstTestModel> valueList = JsonExtensions.DeserializeCustom<List<FirstTestModel>>(RepoValue);
            if (valueList.Count > 0)
            {
                result.Tag = 1;
                result.Data = valueList;
                result.Message = "获取成功";
            }
            return result;
        }
    }
}
