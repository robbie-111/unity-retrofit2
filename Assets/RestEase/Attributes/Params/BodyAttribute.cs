using System;

namespace RestEase.Attributes.Params
{
    /// <summary>
    /// 파라미터를 JSON 직렬화하여 요청 바디(application/json)로 전송한다.
    /// 메서드 당 하나만 허용.
    /// <example>[Post("users")] Task CreateAsync([Body] UserRequest req);</example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class BodyAttribute : Attribute { }
}
