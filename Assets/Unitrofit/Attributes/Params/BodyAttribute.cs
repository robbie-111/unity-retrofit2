using System;

namespace Unitrofit.Attributes.Params
{
    /// <summary>
    /// 요청 바디. JSON으로 직렬화되어 전송된다.
    /// [Post("/users")] void Create([Body] UserRequest req)
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class BodyAttribute : Attribute { }
}
