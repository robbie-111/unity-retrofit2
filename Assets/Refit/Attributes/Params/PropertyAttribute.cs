using System;

namespace Refit.Attributes.Params
{
    /// <summary>
    /// 런타임 상태값을 UnityWebRequest 의 커스텀 딕셔너리로 전달한다.
    /// DelegatingHandler / RequestModifier 내에서 이 값을 읽어 동적 동작을 구현할 수 있다.
    ///
    /// <para>key 를 생략하면 파라미터 이름이 키로 사용된다.</para>
    ///
    /// <example>
    /// [Post("/users")]
    /// Task CreateAsync([Body] User user, [Property("TraceId")] string traceId);
    ///
    /// // RequestModifier 에서 읽기:
    /// // if (customProps.TryGetValue("TraceId", out var id)) { ... }
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class PropertyAttribute : Attribute
    {
        /// <summary>커스텀 딕셔너리 키. null 이면 파라미터 이름 사용.</summary>
        public string Key { get; }
        public PropertyAttribute(string key = null) { Key = key; }
    }
}
