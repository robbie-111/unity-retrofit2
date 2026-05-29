using System;

namespace Unitrofit.Attributes.Http
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [HttpMethod(HttpMethod.Post)]
    public sealed class PostAttribute : Attribute
    {
        public string Path { get; }
        public PostAttribute(string path = "") { Path = path; }
    }
}
