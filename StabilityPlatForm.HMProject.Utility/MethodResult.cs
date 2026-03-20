namespace StabilityPlatForm.HMProject.Utility
{
    public class MethodResult<T>
    {
        public T Data { get; }
        public string Msg { get; }
        public bool IsSuccessful { get; }


        private MethodResult(bool isSuccessful, string msg, T data)
        {
            Data = data;
            Msg = msg ?? string.Empty;
            IsSuccessful = isSuccessful;
        }

        public static MethodResult<T> Success(T data = default, string message = "")
        {
            return new MethodResult<T>(true, message, data);
        }

        public static MethodResult<T> Fail(string message = "")
        {
            return new MethodResult<T>(false, message, default);
        }
    }
}
