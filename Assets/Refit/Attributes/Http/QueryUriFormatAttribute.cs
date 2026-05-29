using System;

namespace Refit.Attributes.Http
{
    /// <summary>
    /// 쿼리 파라미터 값의 URL 이스케이핑 방식을 제어한다.
    /// 기본값은 <see cref="UriFormat.UriEscaped"/> (표준 URL 인코딩).
    /// <see cref="UriFormat.Unescaped"/> 로 설정하면 '+', '/' 등이 인코딩되지 않는다.
    ///
    /// <example>
    /// [Get("/query")]
    /// [QueryUriFormat(UriFormat.Unescaped)]
    /// Task QueryAsync(string q);
    /// // QueryAsync("Select+Id,Name") => /query?q=Select+Id,Name
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class QueryUriFormatAttribute : Attribute
    {
        public UriFormat Format { get; }
        public QueryUriFormatAttribute(UriFormat format = UriFormat.UriEscaped) { Format = format; }
    }
}
