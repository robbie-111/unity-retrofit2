using System;

namespace RestEase.Attributes.Http
{
    /// <summary>
    /// HTTP 2xx 이외의 응답이 와도 ApiException 을 throw하지 않는다.
    /// 인터페이스 레벨에 붙이면 모든 메서드에 적용된다.
    /// <para>
    /// 이 어트리뷰트를 사용할 때는 보통 Task&lt;Response&lt;T&gt;&gt; 를 반환하여
    /// 상태 코드를 직접 확인한다.
    /// </para>
    /// <example>
    /// [Get("users/{id}")]
    /// [AllowAnyStatusCode]
    /// Task&lt;Response&lt;User&gt;&gt; GetUserAsync([Path] int id);
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class AllowAnyStatusCodeAttribute : Attribute { }
}
