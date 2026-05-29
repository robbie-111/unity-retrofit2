using System;

namespace Unitrofit.Attributes.Http
{
    /// <summary>
    /// 인터페이스 또는 메서드 레벨 정적 헤더.
    /// "Key: Value" 형식으로 선언한다.
    /// 값이 없으면 ("Key") 해당 키의 헤더를 제거한다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HeadersAttribute : Attribute
    {
        public string[] Headers { get; }
        public HeadersAttribute(params string[] headers) { Headers = headers ?? new string[0]; }
    }
}
