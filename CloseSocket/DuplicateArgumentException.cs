namespace CloseSocket
{
    [Serializable]
    internal class DuplicateArgumentException : Exception
    {
        public DuplicateArgumentException() : base("Duplicate argument")
        {
        }

        public DuplicateArgumentException(string? arg) : base($"Duplicate argument: '{arg}'")
        {
        }

        public DuplicateArgumentException(string? arg, Exception? innerException) : base($"Duplicate argument: '{arg}'", innerException)
        {
        }
    }
}