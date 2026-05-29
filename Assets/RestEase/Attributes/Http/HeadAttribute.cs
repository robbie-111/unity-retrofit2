using System;

namespace RestEase.Attributes.Http
{
    /// <summary>[Head("path")] — HEAD 요청.</summary>
    [HttpMethod(HttpMethod.Head)]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class HeadAttribute : Attribute
    {
        public string Path { get; }
        public HeadAttribute(string path = "") { Path = path; }
    }
}
