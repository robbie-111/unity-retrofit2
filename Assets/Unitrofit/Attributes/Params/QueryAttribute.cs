using System;

namespace Unitrofit.Attributes.Params
{
    /// <summary>
    /// URL 쿼리 파라미터. ?key=value 형태로 URL에 추가된다.
    /// [Get("/search")] void Search([Query("q")] string keyword)
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class QueryAttribute : Attribute
    {
        public string Name { get; }
        public QueryAttribute(string name = null) { Name = name; }
    }
}
