using System;

namespace RestEase.Attributes.Http
{
    /// <summary>[Delete("path")] — DELETE 요청.</summary>
    [HttpMethod(HttpMethod.Delete)]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class DeleteAttribute : Attribute
    {
        public string Path { get; }
        public DeleteAttribute(string path = "") { Path = path; }
    }
}
