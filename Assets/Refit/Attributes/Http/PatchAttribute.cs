using System;

namespace Refit.Attributes.Http
{
    /// <summary>[Patch("/path")] — PATCH 요청.</summary>
    [HttpMethod(HttpMethod.Patch)]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class PatchAttribute : Attribute
    {
        public string Path { get; }
        public PatchAttribute(string path = "") { Path = path; }
    }
}
