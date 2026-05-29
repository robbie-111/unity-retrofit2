using System;

namespace RestEase.Attributes.Http
{
    public enum HttpMethod { Get, Post, Put, Delete, Patch, Head }

    /// <summary>
    /// HTTP 메서드 어트리뷰트 클래스에 붙이는 메타 어트리뷰트.
    /// [Get], [Post] 등이 어떤 HTTP 동사인지 표시한다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class HttpMethodAttribute : Attribute
    {
        public HttpMethod Method { get; }
        public HttpMethodAttribute(HttpMethod method) { Method = method; }
    }
}
