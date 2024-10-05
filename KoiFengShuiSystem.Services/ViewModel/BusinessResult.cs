using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.ViewModel
{
    public interface IBusinessResult
    {
        int Status { get; set; }
        string? Message { get; set; }
        object? Data { get; set; }
        bool Success { get; }
        string? ErrorMessage { get; }
    }

    public class BusinessResult : IBusinessResult
    {
        public int Status { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }

        // New properties
        public bool Success => Status == 1; // Assuming Status 1 indicates success
        public string? ErrorMessage => Success ? null : Message;

        public BusinessResult()
        {
            Status = -1;
            Message = "Action fail";
        }

        public BusinessResult(int status, string message)
        {
            Status = status;
            Message = message;
        }

        public BusinessResult(int status, string message, object data)
        {
            Status = status;
            Message = message;
            Data = data;
        }
    }
}
