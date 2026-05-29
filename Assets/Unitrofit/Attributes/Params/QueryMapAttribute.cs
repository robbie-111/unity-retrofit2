using System;

namespace Unitrofit.Attributes.Params
{
    /// <summary>
    /// Dictionary&lt;string, string&gt;을 쿼리 파라미터로 일괄 변환한다.
    /// [Get("/search")] void Search([QueryMap] Dictionary&lt;string,string&gt; filters)
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class QueryMapAttribute : Attribute { }
}
