using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TasksWebApi.Requests;

namespace TasksWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TasksController : ControllerBase
    {
        protected IMongoDatabase _database;
        private IMongoCollection<BsonDocument> _tasks;
        protected Guid? UserId => User.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).FirstOrDefault() != null ? (Guid?)Guid.Parse(User.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).First().Value) : null;
        protected string Role => User.Claims.Where(c => c.Type == ClaimTypes.Role).FirstOrDefault() != null ? User.Claims.Where(c => c.Type == ClaimTypes.Role).First().Value : null;

        public TasksController(IMongoDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _tasks = _database.GetCollection<BsonDocument>("Tasks");
        }

        // GET: api/Tasks?filter=status eq done
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery(Name = "$filter")]string filter)
        {
            var bsonFilter = new BsonDocument();
            if (!string.IsNullOrEmpty(filter))
            {
                var conditionArray = filter.Trim().Split(new[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);

                if (string.Equals(conditionArray[1], "eq"))
                {
                    bsonFilter.Set(ToPascalCase(conditionArray[0]), conditionArray[2]);
                }
            }

            if (Role == "User")
            {
                bsonFilter = bsonFilter.Set("UserId", UserId);
            }

            var result = await _tasks.Find(bsonFilter).Project<dynamic>("{_id:0, Summary:1, Status:1, UserId:1}").ToListAsync();
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }

        // GET: api/Tasks/dfcc81de-0871-455e-81cb-b03997df30ad
        [HttpGet("{id}", Name = "Get")]
        public async Task<IActionResult> Get(Guid id)
        {
            var filter = new BsonDocument("_id", id);
            if (Role == "User")
            {
                filter = filter.Set("UserId", UserId);
            }

            var result = await _tasks.Find(filter).Project<dynamic>("{_id:0, Summary:1, Status:1}").SingleOrDefaultAsync();
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }

        // POST: api/Tasks
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CreateTaskRequest request)
        {
            var taskId = Guid.NewGuid();
            var document = BsonSerializer.Deserialize<BsonDocument>(JsonConvert.SerializeObject(request))
                .Set("_id", taskId)
                .Set("UserId", UserId);
            await _tasks.InsertOneAsync(document);
            return Ok(new { Id = taskId });
        }

        // PATCH: api/Tasks/dfcc81de-0871-455e-81cb-b03997df30ad
        [HttpPatch("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody]JsonPatchDocument<ChangeStatusRequest> request)
        {
            if (request.Operations.Any(o => o.path.Contains("/Status")))
            {
                var status = request.Operations.Where(o => o.path.Contains("/Status")).Select(result => result.value.ToString()).SingleOrDefault();
                if (status != null)
                {
                    var filter = new BsonDocument("_id", id);
                    if(Role == "User")
                    {
                        filter.Set("UserId", UserId);
                    }

                    var update = Builders<BsonDocument>.Update
                        .Set("Status", status);
                    await _tasks.FindOneAndUpdateAsync(filter, update);
                }
            }
            return Ok();
        }

        // DELETE: api/Tasks/dfcc81de-0871-455e-81cb-b03997df30ad
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var filter = new BsonDocument("_id", id);
            if (Role == "User")
            {
                filter = filter.Set("UserId", UserId);
            }

            await _tasks.DeleteOneAsync(filter);
            return Ok();
        }

        [NonAction]
        public static string ToPascalCase(string s)
        {
            char[] a = Regex.Replace(s, @"(?<=\.)\w", m => char.ToUpper(m.Value[0]).ToString()).ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }
    }
}
