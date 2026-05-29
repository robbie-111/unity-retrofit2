using System;

namespace RestEase.Attributes.Http
{
    /// <summary>[Put("path")] — PUT 요청.</summary>
    [HttpMethod(HttpMethod.Put)]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class PutAttribute : Attribute
    {
        public string Path { get; }
        public PutAttribute(string path = "") { Path = path; }
    }
}
