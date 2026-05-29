using System;

namespace Unitrofit.Attributes.Http
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [HttpMethod(HttpMethod.Delete)]
    public sealed class DeleteAttribute : Attribute
    {
        public string Path { get; }
        public DeleteAttribute(string path = "") { Path = path; }
    }
}
