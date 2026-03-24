



namespace InvestmentService.Models
{
    public class LoginModel{
        public required string username { get; set; }
        public required string password { get; set; }
    }

    public class ResponseDto<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T? Data { get; set; }
        public string? Error { get; set; }


        public ResponseDto(bool success, string message, T? data = default, string? error = null)
        {
            Success = success;
            Message = message;
            Data = data;
            Error = error;
        }

        public static ResponseDto<T> SuccessResponse(T data, string message = "Success")
            => new ResponseDto<T>(true, message, data);

        public static ResponseDto<T> FailureResponse(string message, string? error = null)
            => new ResponseDto<T>(false, message, default, error);
    }

}


