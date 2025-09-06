namespace staj_proje.Model.Dto
{
    public class ResponseApi<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }

        public static ResponseApi<T> SuccessResponse(T data, string message)
            => new ResponseApi<T> { Success = true, Data = data, Message = message };

        public static ResponseApi<T> ErrorResponse(string message)
            => new ResponseApi<T> { Success = false, Data = default, Message = message };
    }
}
