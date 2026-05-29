using System;

namespace Refit.Attributes.Http
{
    public enum HttpMethod { Get, Post, Put, Delete, Patch, Head, Options }

    /// <summary>
    /// [Get], [Post] 등의 HTTP 메서드 어트리뷰트 클래스에 붙이는 메타 어트리뷰트.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class HttpMethodAttribute : Attribute
    {
        public HttpMethod Method { get; }
        public HttpMethodAttribute(HttpMethod method) { Method = method; }
    }
}
