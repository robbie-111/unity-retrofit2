using System;

namespace Refit.Attributes.Http
{
    /// <summary>
    /// Refit 원본 스타일의 정적 헤더 선언. "Key: Value" 형식의 문자열을 사용한다.
    /// 값이 없으면 해당 헤더를 제거한다. 콜론만 있으면 빈 값으로 설정한다.
    ///
    /// <para>인터페이스 레벨: 모든 메서드에 적용.</para>
    /// <para>메서드 레벨: 해당 메서드에만 적용. 동일 키는 인터페이스 헤더를 덮어씀.</para>
    ///
    /// <example>
    /// [Headers("User-Agent: Awesome App", "Cache-Control: no-cache")]
    /// public interface IGitHubApi { ... }
    ///
    /// [Headers("X-Emoji: :rocket:")]
    /// [Get("/users")]
    /// Task&lt;List&lt;User&gt;&gt; GetUsersAsync();
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HeadersAttribute : Attribute
    {
        public string[] Headers { get; }
        public HeadersAttribute(params string[] headers) { Headers = headers; }
    }
}
