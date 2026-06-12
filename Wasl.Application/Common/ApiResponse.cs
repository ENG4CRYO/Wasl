using System;
using System.Collections.Generic;
using System.Text;

namespace Wasl.Application.Common
{
    public class ApiResponse<T>
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; }
        public T? Data { get; set; }
        public Dictionary<string, List<string>> Errors { get; set; }

        
        public ApiResponse() { }
        public static ApiResponse<T> Success(T data, string message = "")
        {
            return new ApiResponse<T>
            {
                Succeeded = true,
                Message = message,
                Data = data,
                Errors = null!
            };
        }


        public static ApiResponse<T> Failure(string message, Dictionary<string, List<string>> errors = null)
        {
            return new ApiResponse<T>
            {
                Succeeded = false,
                Message = message,
                Data = default, 
                Errors = errors
            };
        }
    }
}
