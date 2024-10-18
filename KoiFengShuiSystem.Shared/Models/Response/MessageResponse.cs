using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.Shared.Models.Response
{
    public class MessageResponse
    {
        public int error { get; set; }
        public string message { get; set; }
        public object? data { get; set; }

        // Parameterless constructor
        public MessageResponse() { }

        // Constructor with three parameters
        public MessageResponse(int error, string message, object? data)
        {
            this.error = error;
            this.message = message;
            this.data = data;
        }
    }
}
