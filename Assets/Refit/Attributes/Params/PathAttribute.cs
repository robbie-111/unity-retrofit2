using System;

namespace Refit.Attributes.Params
{
    /// <summary>
    /// URL 경로 템플릿의 {placeholder} 를 실제 값으로 치환한다.
    /// 이름을 생략하면 파라미터 이름(또는 [AliasAs])을 플레이스홀더 키로 사용한다.
    ///
    /// <example>
    /// [Get("/users/{id}")] Task GetAsync([Path] int id);
    /// [Get("/group/{id}/users")] Task GetAsync([Path("id")] int groupId);
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class PathAttribute : Attribute
    {
        /// <summary>플레이스홀더 이름. null 이면 파라미터 이름 사용.</summary>
        public string Name { get; }
        public PathAttribute(string name = null) { Name = name; }
    }
}
