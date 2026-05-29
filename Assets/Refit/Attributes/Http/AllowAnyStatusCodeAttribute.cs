using System;

namespace Refit.Attributes.Http
{
    /// <summary>
    /// HTTP 2xx 이외의 응답이 와도 <see cref="Core.ApiException"/> 을 throw하지 않는다.
    /// 인터페이스 레벨에 붙이면 모든 메서드에 적용된다.
    /// 보통 <c>Task&lt;ApiResponse&lt;T&gt;&gt;</c> 또는 <c>Task&lt;IApiResponse&lt;T&gt;&gt;</c>
    /// 와 함께 사용하여 상태코드를 직접 확인한다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class AllowAnyStatusCodeAttribute : Attribute { }
}
