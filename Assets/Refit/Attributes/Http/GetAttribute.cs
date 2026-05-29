using System;

namespace Refit.Attributes.Http
{
    /// <summary>[Get("/path")] — GET 요청.</summary>
    [HttpMethod(HttpMethod.Get)]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class GetAttribute : Attribute
    {
        public string Path { get; }
        public GetAttribute(string path = "") { Path = path; }
    }
}
