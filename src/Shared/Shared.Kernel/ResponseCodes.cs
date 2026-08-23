namespace KoiFengShuiSystem.Shared.Kernel
{
    /// <summary>
    /// Canonical response status codes and messages shared by legacy-envelope consumers.
    /// Success codes are positive, failures negative; warnings use positive non-one values.
    /// </summary>
    public static class ResponseCodes
    {
        public const int ErrorException = -4;

        public const int SuccessCreateCode = 1;
        public const string SuccessCreateMessage = "Save data success";
        public const int SuccessReadCode = 1;
        public const string SuccessReadMessage = "Get data success";
        public const int SuccessDeleteCode = 1;
        public const string SuccessDeleteMessage = "Delete data success";

        public const int FailCreateCode = -1;
        public const string FailCreateMessage = "Save data fail";
        public const int FailReadCode = -1;
        public const string FailReadMessage = "Get data fail";

        public const int WarningNoDataCode = 4;
        public const string WarningNoDataMessage = "No data";
    }
}
