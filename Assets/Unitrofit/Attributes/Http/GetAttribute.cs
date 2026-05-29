using System;

namespace Unitrofit.Attributes.Http
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [HttpMethod(HttpMethod.Get)]
    public sealed class GetAttribute : Attribute
    {
        public string Path { get; }
        public GetAttribute(string path = "") { Path = path; }
    }
}
