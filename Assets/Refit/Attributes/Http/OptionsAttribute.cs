using System;

namespace Refit.Attributes.Http
{
    /// <summary>[Options("/path")] — OPTIONS 요청.</summary>
    [HttpMethod(HttpMethod.Options)]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class OptionsAttribute : Attribute
    {
        public string Path { get; }
        public OptionsAttribute(string path = "") { Path = path; }
    }
}
