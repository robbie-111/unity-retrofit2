using System;

namespace Refit.Attributes.Http
{
    /// <summary>[Post("/path")] — POST 요청.</summary>
    [HttpMethod(HttpMethod.Post)]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class PostAttribute : Attribute
    {
        public string Path { get; }
        public PostAttribute(string path = "") { Path = path; }
    }
}
