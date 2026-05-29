using System;

namespace RestEase.Attributes.Params
{
    /// <summary>
    /// Dictionary&lt;string,string&gt; 전체를 쿼리스트링으로 변환한다.
    /// <example>[Get("search")] Task SearchAsync([QueryMap] Dictionary&lt;string,string&gt; filters);</example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class QueryMapAttribute : Attribute { }
}
