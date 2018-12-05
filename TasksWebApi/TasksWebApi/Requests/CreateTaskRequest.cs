using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TasksWebApi.Requests
{
    public class CreateTaskRequest
    {
        public string Summary { get; set; }
        public string Status { get; set; }
    }
}
