namespace SmartCache.Application.Exceptions
{
    public class VersionNotModifiedException:Exception
    {
        public VersionNotModifiedException() : base() { }

        public VersionNotModifiedException(string message) : base(message) { }

        public VersionNotModifiedException(string message, Exception innerException) : base(message, innerException) { }
    }
}
