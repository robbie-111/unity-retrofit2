using System;

namespace Unitrofit.Attributes.Http
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [HttpMethod(HttpMethod.Put)]
    public sealed class PutAttribute : Attribute
    {
        public string Path { get; }
        public PutAttribute(string path = "") { Path = path; }
    }
}
