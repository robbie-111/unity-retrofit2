using System;

namespace Unitrofit.Attributes.Http
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [HttpMethod(HttpMethod.Patch)]
    public sealed class PatchAttribute : Attribute
    {
        public string Path { get; }
        public PatchAttribute(string path = "") { Path = path; }
    }
}
