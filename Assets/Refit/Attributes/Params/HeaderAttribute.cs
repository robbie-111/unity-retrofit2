using System;

namespace Refit.Attributes.Params
{
    /// <summary>
    /// 동적 헤더 값을 파라미터로 전달한다.
    /// 파라미터 값이 null 이면 해당 헤더가 제거된다.
    ///
    /// <para>우선순위: [Header] 파라미터 &gt; [Headers] 메서드 &gt; [Headers] 인터페이스</para>
    ///
    /// <example>
    /// [Get("/users/{user}")]
    /// Task&lt;User&gt; GetUserAsync(string user, [Header("Authorization")] string authorization);
    ///
    /// // await GetUserAsync("octocat", "token OAUTH-TOKEN");
    /// // => Authorization: token OAUTH-TOKEN
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class HeaderAttribute : Attribute
    {
        public string Name { get; }
        public HeaderAttribute(string name) { Name = name; }
    }
}
