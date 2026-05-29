using System;

namespace Refit.Attributes.Params
{
    /// <summary>
    /// <c>IDictionary&lt;string, string&gt;</c> 파라미터를 여러 헤더로 한 번에 주입한다.
    /// 개별 [Header] 파라미터를 여러 번 선언하는 번거로움을 줄여준다.
    ///
    /// <example>
    /// [Get("/users/{user}")]
    /// Task&lt;User&gt; GetUserAsync(string user, [HeaderCollection] IDictionary&lt;string, string&gt; headers);
    ///
    /// var headers = new Dictionary&lt;string, string&gt;
    /// {
    ///     { "Authorization", "Bearer tokenGoesHere" },
    ///     { "X-Tenant-Id", "123" }
    /// };
    /// await GetUserAsync("octocat", headers);
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class HeaderCollectionAttribute : Attribute { }
}
