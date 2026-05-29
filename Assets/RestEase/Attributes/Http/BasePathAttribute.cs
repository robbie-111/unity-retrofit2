using System;

namespace RestEase.Attributes.Http
{
    /// <summary>
    /// 인터페이스 내 모든 메서드 경로의 공통 접두사를 지정한다.
    /// <para>메서드 경로가 '/'로 시작하면 BasePath는 무시된다.</para>
    /// <example>
    /// [BasePath("api/v1")]
    /// public interface IMyApi
    /// {
    ///     [Get("users")]          // → baseUrl/api/v1/users
    ///     Task&lt;List&lt;User&gt;&gt; GetUsersAsync();
    ///
    ///     [Get("/health")]        // → baseUrl/health  (BasePath 무시)
    ///     Task PingAsync();
    /// }
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public sealed class BasePathAttribute : Attribute
    {
        public string Path { get; }
        public BasePathAttribute(string path) { Path = path; }
    }
}
