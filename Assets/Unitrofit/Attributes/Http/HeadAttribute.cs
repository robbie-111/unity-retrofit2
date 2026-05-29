using System;

namespace Unitrofit.Attributes.Http
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [HttpMethod(HttpMethod.Head)]
    public sealed class HeadAttribute : Attribute
    {
        public string Path { get; }
        public HeadAttribute(string path = "") { Path = path; }
    }
}
