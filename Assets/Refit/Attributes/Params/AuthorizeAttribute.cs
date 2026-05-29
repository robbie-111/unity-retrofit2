using System;

namespace Refit.Attributes.Params
{
    /// <summary>
    /// Authorization 헤더의 단축 어트리뷰트.
    /// <c>[Header("Authorization")]</c> 와 달리, scheme 을 어트리뷰트에서 지정하고
    /// 파라미터에는 토큰 값만 전달한다.
    ///
    /// <para>scheme 을 생략하면 기본값 "Bearer" 가 사용된다.</para>
    ///
    /// <example>
    /// // Bearer scheme 명시
    /// [Get("/users/{user}")]
    /// Task&lt;User&gt; GetUserAsync(string user, [Authorize("Bearer")] string token);
    /// // await GetUserAsync("octocat", "OAUTH-TOKEN");
    /// // => Authorization: Bearer OAUTH-TOKEN
    ///
    /// // scheme 생략 (기본: Bearer)
    /// Task&lt;User&gt; GetUserAsync(string user, [Authorize] string token);
    /// // => Authorization: Bearer &lt;token&gt;
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class AuthorizeAttribute : Attribute
    {
        public const string DefaultScheme = "Bearer";
        public string Scheme { get; }
        public AuthorizeAttribute(string scheme = DefaultScheme) { Scheme = scheme; }
    }
}
