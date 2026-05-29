using System;

namespace RestEase.Attributes.Params
{
    /// <summary>
    /// 동적 헤더 값을 파라미터로 전달한다.
    /// 정적 헤더는 [Header("Key","Value")] 어트리뷰트를 사용한다.
    /// <example>Task GetAsync([HeaderParam("Authorization")] string token);</example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class HeaderParamAttribute : Attribute
    {
        public string Name { get; }
        public HeaderParamAttribute(string name) { Name = name; }
    }
}
