using System;

namespace Unitrofit.Attributes.Http
{
    /// <summary>
    /// HTTP 메서드를 지정하는 어트리뷰트의 마커.
    /// [Get], [Post] 등의 어트리뷰트 클래스에 이 어트리뷰트를 붙여 메서드를 식별한다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class HttpMethodAttribute : Attribute
    {
        public HttpMethod Method { get; }
        public HttpMethodAttribute(HttpMethod method) { Method = method; }
    }

    public enum HttpMethod
    {
        Get, Post, Put, Patch, Delete, Head,
    }
}
