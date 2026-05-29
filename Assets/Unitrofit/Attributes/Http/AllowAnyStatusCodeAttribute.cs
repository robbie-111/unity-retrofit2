using System;

namespace Unitrofit.Attributes.Http
{
    /// <summary>
    /// 비-2xx 응답에서도 예외를 throw하지 않는다.
    /// 인터페이스 레벨 또는 메서드 레벨에 적용 가능하다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class AllowAnyStatusCodeAttribute : Attribute { }
}
