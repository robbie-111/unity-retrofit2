using System;

namespace RestEase.Attributes.Params
{
    /// <summary>
    /// URL 쿼리스트링 파라미터. name 생략 시 파라미터 이름을 키로 사용한다.
    /// <example>[Get("users")] Task GetAsync([Query("page")] int page);</example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class QueryAttribute : Attribute
    {
        /// <summary>쿼리 키. null 이면 파라미터 이름을 사용.</summary>
        public string Name { get; }
        public QueryAttribute(string name = null) { Name = name; }
    }
}
