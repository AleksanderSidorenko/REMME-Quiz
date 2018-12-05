using Newtonsoft.Json;
using System;

namespace TasksWebApi
{
    public class User
    {
        public Guid Id { get; set; }
        public string Role { get; set; }
        public string Username { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Password { get; set; }
        public string Token { get; set; }
    }
}
