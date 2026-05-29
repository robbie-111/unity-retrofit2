using System;

namespace RestEase.Attributes.Params
{
    /// <summary>
    /// URL 경로 템플릿의 {placeholder} 를 실제 값으로 치환한다.
    /// name 생략 시 파라미터 이름을 플레이스홀더 키로 사용한다.
    /// <example>[Get("users/{id}")] Task GetAsync([Path("id")] int id);</example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class PathAttribute : Attribute
    {
        /// <summary>플레이스홀더 이름. null 이면 파라미터 이름을 사용.</summary>
        public string Name { get; }
        public PathAttribute(string name = null) { Name = name; }
    }
}
